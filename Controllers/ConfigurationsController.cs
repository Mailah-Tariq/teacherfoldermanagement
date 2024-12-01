using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using TeacherFolderManagement.Models;
using System.Data;
using System.Text;
using System.Security.Cryptography.Xml;
using teacherFolderManagment.Models;
using System.Web.Http.Cors;


namespace TeacherFolderManagementSystem.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ConfigurationsController : ApiController
    {
        TeacherFolderManagementEntities db = new TeacherFolderManagementEntities();

        [HttpPost]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage UploadNewStudents()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                //check for the file
                if (httpRequest.Files.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Excel File is missing");

                var fileName = httpRequest.Files[0].FileName;

                var ext = fileName.Split(new char[] { '.' })[1];

                if (!"xls,xlsx".Contains(ext.ToLower()))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid Excel File");

                var tempFileName = DateTime.Now.ToFileTime() + "." + ext;

                var filePathName = HttpContext.Current.Server.MapPath($"~/temp/{tempFileName}");

                httpRequest.Files[0].SaveAs(filePathName);

                //now read the excel file.

                //1. set connection string
                var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePathName};Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";
                //2.now create connection to excel sheet

                var excelConnection = new OleDbConnection(connectionString);
                excelConnection.Open();
                //check schema of the excel file
                var tablesInExcel = excelConnection.GetSchema("Tables");
                if (tablesInExcel.Rows.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid Excel File");

                var sheetName = tablesInExcel.Rows[0]["TABLE_NAME"].ToString();

                //3. now create command and execute query to get data from sheet.
                DataTable dtData = new DataTable();
                var query = $"select * from [{sheetName}]";
                var adapter = new OleDbDataAdapter(query, excelConnection);
                adapter.Fill(dtData);
                excelConnection.Close();

                DataRow firstRow = dtData.Rows[0];
                for (int i = 0; i < dtData.Columns.Count; i++)
                {
                    dtData.Columns[i].ColumnName = firstRow[i].ToString();
                }
                var errorList = new StringBuilder();
                //check the required columns
                if (!dtData.Columns.Contains("RegNo"))
                    errorList.AppendLine("RegNo is missing");
                if (!dtData.Columns.Contains("FirstName"))
                    errorList.AppendLine("FirstName is missing");
                if (!dtData.Columns.Contains("LastName"))
                    errorList.AppendLine("LastName is missing");
                if (!dtData.Columns.Contains("FatherName"))
                    errorList.AppendLine("FatherName is missing");
                if (!dtData.Columns.Contains("Gender"))
                    errorList.AppendLine("Gender is missing");
                if (!dtData.Columns.Contains("ProgramShortName"))
                    errorList.AppendLine("Program is missing");
                if (!dtData.Columns.Contains("SOSYear"))
                    errorList.AppendLine("SOSYear is missing");

                var errorMessage = errorList.ToString();
                if (!String.IsNullOrEmpty(errorMessage))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, errorMessage);


                var newStudents = dtData.AsEnumerable().Skip(1).Select(row => new NewStudentSchema
                {
                    regNo = row["RegNo"].ToString(),
                    firstName = row["FirstName"].ToString(),
                    lastName = row["LastName"].ToString(),
                    fatherName = row["FatherName"].ToString(),
                    gender = row["Gender"].ToString(),
                    programName = row["ProgramShortName"].ToString(),
                    sosYearName = Convert.ToInt32(row["SOSYear"])
                }).ToList();

                // 

                var notAddedStudents = new List<NewStudentSchema>();

                //now read row by row.
                foreach (var student in newStudents)
                {
                    //first check that program is valid and if then get its id from db.
                    var program = db.Program.FirstOrDefault(p => p.ShortName == student.programName);
                    if (program == null)
                    {
                        notAddedStudents.Add(student);
                        continue;
                    }

                    //check for sosyear
                    var sos = db.SchemeOfStudy.FirstOrDefault(s => s.ProgramId == program.Id && s.Year == student.sosYearName);
                    if (sos == null)
                    {
                        notAddedStudents.Add(student);
                        continue;
                    }

                    // Check if the student already exists in the database
                    var existingStudent = db.Student.FirstOrDefault(s => s.AridNo == student.regNo);
                    if (existingStudent != null)
                    {
                        notAddedStudents.Add(student);
                        continue;
                    }


                    var dbStudent = new Student
                    {
                        AridNo = student.regNo,
                        FirstName = student.firstName,
                        LastName = student.lastName,
                        FatherName = student.fatherName,
                        Gender = student.gender,
                        SOSId = sos.Id
                    };

                    db.Student.Add(dbStudent);
                }
                db.SaveChanges();//commit to db.

                if (notAddedStudents.Any())
                {
                    if (notAddedStudents.Count != newStudents.Count)
                        return Request.CreateResponse(HttpStatusCode.OK, notAddedStudents);
                    //if all are rejected.
                    return Request.CreateResponse(HttpStatusCode.BadRequest, notAddedStudents);
                }


                return Request.CreateResponse(HttpStatusCode.OK, "All added.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        ///////////////////////////////////////////////////////////////////
        ///

        [HttpPost]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage UploadNewCourseAllocation()
        {
            try
            {
                var useDefaultSession = false;


                var httpRequest = HttpContext.Current.Request;
                //check for the file
                if (httpRequest.Files.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Excel File is missing");

                var fileName = httpRequest.Files[0].FileName;

                var ext = fileName.Split(new char[] { '.' })[1];

                if (!"xls,xlsx".Contains(ext.ToLower()))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid Excel File");

                var tempFileName = DateTime.Now.ToFileTime() + "." + ext;

                var filePathName = HttpContext.Current.Server.MapPath($"~/temp/{tempFileName}");

                httpRequest.Files[0].SaveAs(filePathName);

                //now read the excel file.

                //1. set connection string
                var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePathName};Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";
                //2.now create connection to excel sheet

                var excelConnection = new OleDbConnection(connectionString);
                excelConnection.Open();
                //check schema of the excel file
                var tablesInExcel = excelConnection.GetSchema("Tables");
                if (tablesInExcel.Rows.Count == 0)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid Excel File");

                var sheetName = tablesInExcel.Rows[0]["TABLE_NAME"].ToString();

                //3. now create command and execute query to get data from sheet.
                DataTable dtData = new DataTable();
                var query = $"select * from [{sheetName}]";
                var adapter = new OleDbDataAdapter(query, excelConnection);
                adapter.Fill(dtData);
                excelConnection.Close();

                DataRow firstRow = dtData.Rows[0];
                for (int i = 0; i < dtData.Columns.Count; i++)
                {
                    dtData.Columns[i].ColumnName = firstRow[i].ToString();
                }
                var errorList = new StringBuilder();
                //check the required columns
                if (!dtData.Columns.Contains("CourseCode"))
                    errorList.AppendLine("CourseCode is missing");
                if (!dtData.Columns.Contains("Teacher"))
                    errorList.AppendLine("Teacher is missing");
                if (!dtData.Columns.Contains("SemesterNo"))
                    errorList.AppendLine("SemesterNo is missing");
                if (!dtData.Columns.Contains("Section"))
                    errorList.AppendLine("Section is missing");
                if (!dtData.Columns.Contains("Program"))
                    errorList.AppendLine("Program is missing");
                if (!dtData.Columns.Contains("Session"))
                    useDefaultSession = true;
                //    errorList.AppendLine("Session is missing");

                var errorMessage = errorList.ToString();
                if (!String.IsNullOrEmpty(errorMessage))
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, errorMessage);


                var newAllocation = dtData.AsEnumerable().Skip(1).Select(row => new NewAllocationSchema
                {
                    CourseCode = row["CourseCode"].ToString(),
                    Teacher = row["Teacher"].ToString(),
                    SemesterNo = row["SemesterNo"].ToString(),
                    Section = row["Section"].ToString(),
                    Session = useDefaultSession ? "" : row["Session"].ToString(),
                    Program = row["Program"].ToString()
                }).Where(row => !String.IsNullOrEmpty(row.CourseCode)).ToList();

                // 

                //var notAddedAllocation = new List<NewAllocationSchema>();
                var notAddedAllocationLog = new List<AllocationLog>();

                //now read row by row.
                foreach (var alloc in newAllocation)
                {
                    var courseDetail = db.Course.FirstOrDefault(c => c.CourseCode.Contains(alloc.CourseCode));
                    if (courseDetail == null)
                    {
                        //course not found 
                        notAddedAllocationLog.Add(new AllocationLog { notAllocated = alloc, Message = "Course Code missing" });
                        continue;
                    }

                    var sosId = db.Program.Where(p => p.ShortName == alloc.Program)
                        .Join(db.SchemeOfStudy.Where(s => s.ActiveStatus == true),
                        p => p.Id, s => s.ProgramId, (p, s) => s.Id).FirstOrDefault();

                    var sos = db.SchemeOfStudy.Where(s => s.Id == sosId)
                                .Join(db.CourseInSOS.Where(c => c.CourseId == courseDetail.Id),
                                s => s.Id, c => c.SoSId, (s, c) => c)
                                .FirstOrDefault();
                    if (sos == null)
                    {
                        //missing
                        notAddedAllocationLog.Add(new AllocationLog { notAllocated = alloc, Message = $"SOS missing Program:{alloc.Program},Course:{courseDetail.Id}" });
                        continue;
                    }

                    //this id will be used.
                    //sos.Id;


                    var sectionDetail = db.Section.FirstOrDefault(s => s.Title == alloc.Section);
                    if (sectionDetail == null)
                    {
                        notAddedAllocationLog.Add(new AllocationLog { notAllocated = alloc, Message = "Section Detail missing" });
                        continue;
                    }

                    // Check if the student already exists in the database
                    var existingUser = db.User.FirstOrDefault(s => s.Username == alloc.Teacher);
                    if (existingUser == null)
                    {
                        notAddedAllocationLog.Add(new AllocationLog { notAllocated = alloc, Message = "Teacher Detail missing" });
                        continue;
                    }

                    var sessionId = 0;
                    if (alloc.Session == "")
                    {
                        sessionId = db.Session.FirstOrDefault(s => s.Flag == true).Id;
                    }
                    var sem = Convert.ToInt32(alloc.SemesterNo);
                    var existingAllocation = db.Allocation.FirstOrDefault(a =>
                        a.CourseInSOSId == sos.Id &&
                         a.UserId == existingUser.Id &&
                         a.SemesterNo == sem &&
                        a.SectionId == sectionDetail.Id &&
                            a.SessionId == sessionId
   );

                    if (existingAllocation != null)
                    {
                        notAddedAllocationLog.Add(new AllocationLog { notAllocated = alloc, Message = "Duplicate allocation found" });
                        continue;
                    }

                    var dbAlloc = new Allocation
                    {
                        CourseInSOSId = sos.Id,
                        UserId = existingUser.Id,
                        SemesterNo = Convert.ToInt32(alloc.SemesterNo),
                        SectionId = sectionDetail.Id,
                        SessionId = sessionId
                    };

                    db.Allocation.Add(dbAlloc);
                }
                db.SaveChanges();//commit to db.

                if (notAddedAllocationLog.Any())
                {
                    if (notAddedAllocationLog.Count !=newAllocation.Count)
                        return Request.CreateResponse(HttpStatusCode.OK, notAddedAllocationLog);
                    //if all are rejected.
                    return Request.CreateResponse(HttpStatusCode.BadRequest, notAddedAllocationLog);
                }


                return Request.CreateResponse(HttpStatusCode.OK, "All added.");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);

            }

        }

        //[HttpPost]
        //public HttpResponseMessage UploadCourseTopic(int courseId)
        //{
        //    try
        //    {
        //        var httpRequest = HttpContext.Current.Request;
        //        //check for the file
        //        if (httpRequest.Files.Count == 0)
        //            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Excel File is missing");

        //        var fileName = httpRequest.Files[0].FileName;

        //        var ext = fileName.Split(new char[] { '.' })[1];

        //        if (!"xls,xlsx".Contains(ext.ToLower()))
        //            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid Excel File");

        //        var tempFileName = DateTime.Now.ToFileTime() + "." + ext;

        //        var filePathName = HttpContext.Current.Server.MapPath($"~/temp/{tempFileName}");

        //        httpRequest.Files[0].SaveAs(filePathName);

        //        //now read the excel file.

        //        //1. set connection string
        //        var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={filePathName};Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";
        //        //2.now create connection to excel sheet

        //        var excelConnection = new OleDbConnection(connectionString);
        //        excelConnection.Open();
        //        //check schema of the excel file
        //        var tablesInExcel = excelConnection.GetSchema("Tables");
        //        if (tablesInExcel.Rows.Count == 0)
        //            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Invalid Excel File");

        //        var sheetName = tablesInExcel.Rows[0]["TABLE_NAME"].ToString();

        //        //3. now create command and execute query to get data from sheet.
        //        DataTable dtData = new DataTable();
        //        var query = $"select * from [{sheetName}]";
        //        var adapter = new OleDbDataAdapter(query, excelConnection);
        //        adapter.Fill(dtData);
        //        excelConnection.Close();

        //        DataRow firstRow = dtData.Rows[0];
        //        for (int i = 0; i < dtData.Columns.Count; i++)
        //        {
        //            dtData.Columns[i].ColumnName = firstRow[i].ToString();
        //        }
        //        var errorList = new StringBuilder();
        //        //check the required columns
        //        if (!dtData.Columns.Contains("Title"))
        //            errorList.AppendLine("Title is missing");
        //        if (!dtData.Columns.Contains("Level"))
        //            errorList.AppendLine("Level is missing");
        //        if (!dtData.Columns.Contains("WeekNo"))
        //            errorList.AppendLine("Week is missing");

        //        var errorMessage = errorList.ToString();
        //        if (!String.IsNullOrEmpty(errorMessage))
        //            return Request.CreateErrorResponse(HttpStatusCode.NotFound, errorMessage);


        //        var newCourseTopic = dtData.AsEnumerable().Skip(1).Select(row => new NewCourseTopicScehema
        //        {
        //            Title = row["Title"].ToString(),
        //            Level = Convert.ToInt32(row["Level"].ToString()),
        //            Week = Convert.ToInt32(row["WeekNo"].ToString())
        //        }).ToList();

        //        // 

        //        var notAddedTopics = new List<NewCourseTopicScehema>();

        //        var topicId = 0;

        //        //now read row by row.
        //        foreach (var topicsRow in newCourseTopic)
        //        {
        //            if (topicsRow.Level == 0)
        //            {
        //                //add new topic.
        //                var topic = db.MasterTopic.Add(new MasterTopic
        //                {
        //                    Name = topicsRow.Title,
        //                    CourseId = courseId
        //                });
        //                db.MasterTopic.Add(topic);
        //                db.SaveChanges();
        //                topicId = topic.Id;
        //            }
        //            else
        //            {
        //                var subTopic = db.MasterSubTopic.Add(new MasterSubTopic
        //                {
        //                    Name = topicsRow.Title,
        //                    TopicId = topicId,
        //                    WeekNo = topicsRow.Week
        //                });
        //            }
        //        }
        //        db.SaveChanges();

        //        if (notAddedTopics.Any())
        //        {
        //            if (notAddedTopics.Count != newCourseTopic.Count)
        //                return Request.CreateResponse(HttpStatusCode.OK, notAddedTopics);
        //            //if all are rejected.
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, notAddedTopics);
        //        }

        //        return Request.CreateResponse(HttpStatusCode.OK, "All added.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);

        //    }

        //}


    }
}