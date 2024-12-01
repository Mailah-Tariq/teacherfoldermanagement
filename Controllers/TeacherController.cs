using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Results;
using teacherFolderManagment.Models;

namespace TeacherFolderManagement.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class TeacherController : ApiController
    {
        TeacherFolderManagementEntities db = new TeacherFolderManagementEntities();

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        //[Route("api/teacher/{id}/courses")]
        public HttpResponseMessage GetTeacherCourses(int id)
        {
            try
            {
                var courses = (from a in db.Allocation
                               join c in db.CourseInSOS on a.CourseInSOSId equals c.Id
                               join ss in db.SchemeOfStudy on c.SoSId equals ss.Id
                               join p in db.Program on ss.ProgramId equals p.Id
                               join s in db.Session on a.SessionId equals s.Id
                               where a.UserId == id && s.Flag == true

                               where a.UserId == id
                               select new
                               {
                                   CourseId = c.CourseId,
                                   CourseTitle = c.Course.Title,
                                   // SectionTitle = a.Section.Title,
                                   SemesterNo = a.SemesterNo,
                                   ProgramShortName = p.ShortName,
                                   ProgramId = p.Id,
                                   CourseInSOSId = a.CourseInSOSId,
                                   CourseCode=c.Course.CourseCode,

                               }).Distinct().ToList();
                if (!courses.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "No courses found for the specified teacher." });
                }

                return Request.CreateResponse(HttpStatusCode.OK, courses);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "An error occurred while processing the request.", Details = ex.Message });
            }
        }

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetTeacherCourseSections(int teacherId, int courseInSOSId)
        {
            try
            {
                var sections = (from a in db.Allocation
                                join c in db.CourseInSOS on a.CourseInSOSId equals c.Id
                                join ss in db.SchemeOfStudy on c.SoSId equals ss.Id
                                join p in db.Program on ss.ProgramId equals p.Id
                                join s in db.Session on a.SessionId equals s.Id
                                where a.UserId == teacherId
                                      && c.Id == courseInSOSId
                                      && s.Flag == true
                                select new
                                {
                                    SectionTitle = a.Section.Title,
                                    CourseInSOSId = c.Id,
                                    CourseId=c.CourseId,
                                    CourseCode=c.Course.CourseCode
                                }).Distinct().ToList();

                if (!sections.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "No sections found for the specified course." });
                }

                return Request.CreateResponse(HttpStatusCode.OK, sections);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "An error occurred while processing the request.", Details = ex.Message });
            }
        }


        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetMainFolder(int courseInSOSId, string ProgramShortName )
        {
            try
            {
                var folders = (
                    from p in db.Program
                    join sos in db.SchemeOfStudy on p.Id equals sos.ProgramId
                    join c in db.CourseInSOS on sos.Id equals c.SoSId
                    join a in db.Allocation on c.Id equals a.CourseInSOSId
                    join course in db.Course on c.CourseId equals course.Id
                    join s in db.Section on a.SectionId equals s.Id
                    join ses in db.Session on a.SessionId equals ses.Id
                    join f in db.Folder on a.Id equals f.AllocationId
                    where c.Id == courseInSOSId
                          && p.ShortName == ProgramShortName
                          && ses.Flag == true
                          && f.Type == "M"
                          && sos.ActiveStatus == true // only active programs are come
                    select new
                    {
                        CourseId = c.CourseId,
                        CourseTitle = course.Title,
                        SectionTitle = s.Title,
                        MainFolder = f.Type,
                        Program = p.ShortName,
                        a.UserId
                    }).ToList();

                if (folders.Count == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "No Main Folder found for the specified course." });
                }

                return Request.CreateResponse(HttpStatusCode.OK, folders);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "An error occurred while processing the request.", Details = ex.Message });
            }
        }

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetProgramPLO(int progId)
        {
            try
            {
                var ploList = db.PLO
                    .Where(a => a.ProgramId == progId)
                    .Select(a => new
                    {
                        Description = a.Description
                    })
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, ploList);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetFolderCheckList()
        {
            db.Configuration.ProxyCreationEnabled = false;
            try
            {
                var folderCheckLists = db.FolderCheckList.Where(f => f.Flag == 1).ToList();

                if (folderCheckLists == null || !folderCheckLists.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No folder checklists found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, folderCheckLists);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while retrieving folder checklists: " + ex.Message);
            }
        }
        //Login Function
        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage Login(string username, string password)
        {
            var user = db.User.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("User not found")
                };
            }

            // Check if the user is active
            if (user.IsActive == true)
            {
                if (user.Password.Contains(password))
                {
                    var roles = user.UserRole.Select(ur => ur.Role).ToList();

                    var roleType = roles.Select(r =>
                    {
                        if (r.Id == 1) return "HOD";
                        else if (r.Id == 2) return "Teacher";
                        else if (r.Id == 3) return "Admin";
                        return null;
                    }).Where(r => r != null).ToList();

                    var allocation = db.Allocation.FirstOrDefault(a => a.UserId == user.Id);

                    int? sessionId = null;
                    string sessionTitle = null;
                    int? courseInSOSId = null;
                    if (allocation != null)
                    {
                        var activeSession = db.Session.FirstOrDefault(s => s.Id == allocation.SessionId && s.Flag == true);
                        sessionId = activeSession?.Id;
                        sessionTitle = activeSession?.Title;
                        courseInSOSId = allocation.CourseInSOSId; // Get courseInSOSId
                    }

                    var program = db.Program.FirstOrDefault();

                    var responseData = new
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Role = roleType,
                        Username = user.Username,
                        SessionId = sessionId,
                        sessionTitle = sessionTitle,
                        ProgramId = program?.Id,
                        CourseInSOSId = courseInSOSId, // Add courseInSOSId to response
                        CourseCode = allocation.CourseInSOS.Course.CourseCode,
                        CourseId = allocation.CourseInSOS.Course.Id,
                    };

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(responseData), System.Text.Encoding.UTF8, "application/json")
                    };
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        Content = new StringContent("Invalid password")
                    };
                }
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("User is not active")
                };
            }
        }


        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetCourseCLO(int courseInSOSId)
        {
            db.Configuration.ProxyCreationEnabled = false; 
            try
            {
                var cloList = db.CLO
                    .Where(a => a.CourseInSOSId == courseInSOSId)
                    .Select(a => new
                    {
                        Description = a.Description
                    })
                    .ToList();

                if (cloList == null || !cloList.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No CLOs found for the specified CourseInSOSId.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, cloList);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while retrieving CLOs: " + ex.Message);
            }
        }
        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetMainFolderChecklist(int courseInSOSId)
        {
            try
            {

                var checklist = db.FolderCheckList.Where(f => f.Flag == 0).OrderBy(f => f.Name).ToList();

                var checklistWithFolder = checklist.Join(db.FolderContent, a => a.Id, b => b.FolderCheckListId, (a, b) =>
                new { a.Id, a.Name, FolderContentId = b.Id }).ToList();

                var checklistWithContents = checklistWithFolder.Join(db.FolderContentDocument.Where(d => d.CourseInSOSId == courseInSOSId), a => a.FolderContentId, b => b.FolderContentId,
                    (a, b) => new { a.Id, a.Name, b.DisplayName, b.FilePath });

                var checklistContentGroups = checklistWithContents.GroupBy(a => new { a.Id, a.Name })
                    .Select(g => new { g.Key.Id, g.Key.Name, Contents = g.Select(v => new { v.DisplayName, v.FilePath }) });

                return Request.CreateResponse(HttpStatusCode.OK, checklistContentGroups);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetFolderMainCheckList()
        {
            db.Configuration.ProxyCreationEnabled = false;
            try
            {
                var folderCheckLists = db.FolderCheckList.Where(f => f.Flag == 0).ToList();

                if (folderCheckLists == null || !folderCheckLists.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No folder checklists found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, folderCheckLists);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while retrieving folder checklists: " + ex.Message);
            }
        }
        //[HttpGet]
        //public HttpResponseMessage GetTopicsByCourseCode( string  CourseCode)
        //{
        //    try
        //    {
        //        var topics = db.Topic
        //            .Where(t => t.Course.CourseCode == CourseCode)
        //            .Select(t => new
        //            {
        //                TopicId = t.Id,
        //                TopicName = t.Name,
        //                TopicFlag = t.fl,
        //                SubTopics = db.SubTopic
        //                    .Where(st => st.TopicId == t.Id)
        //                    .Select(st => new
        //                    {
        //                        courseId=t.CourseId,
        //                        SubTopicId = st.Id,
        //                        SubTopicName = st.Name,
        //                        WeekNo = st.WeekNo,
        //                        SubTopicFlag = st.Flag 
        //                    }).ToList()
        //            }).ToList();

        //        return Request.CreateResponse(HttpStatusCode.OK, topics);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "Error retrieving topics", Details = ex.Message });
        //    }
        //}
        //[HttpPost]
        //public HttpResponseMessage UpdateFlagStatus(int id, string type)
        //{
        //    try
        //    {
        //        if (type == "Topic")
        //        {
        //            var topic = db.MasterTopic.FirstOrDefault(t => t.Id == id);
        //            if (topic != null)
        //            {
        //                topic.Flag = true;
        //                db.SaveChanges();
        //                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Topic flag updated successfully" });
        //            }
        //            return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Topic not found" });
        //        }
        //        else if (type == "SubTopic")
        //        {
        //            var subTopic = db.MasterSubTopic.FirstOrDefault(st => st.Id == id);
        //            if (subTopic != null)
        //            {
        //                MasterSubTopic.fl = true;
        //                db.SaveChanges();
        //                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "SubTopic flag updated successfully" });
        //            }
        //            return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "SubTopic not found" });
        //        }
        //        return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Invalid type parameter" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "Error updating flag status", Details = ex.Message });
        //    }
        //}
        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetTopicsByCourseCode(string CourseCode)
        {
            try
            {
                var course = db.Course.FirstOrDefault(c => c.CourseCode == CourseCode);
                if (course == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Course not found" });
                }

                var topics = db.MasterTopic
                    .Where(t => t.CourseId == course.Id)
                    .Select(t => new
                    {
                        TopicId = t.Id,
                        TopicName = t.Name,
                        SubTopics = db.MasterSubTopic
                            .Where(st => st.TopicId == t.Id)
                            .Select(st => new
                            {
                                SubTopicId = st.Id,
                                SubTopicName = st.Name,
                                WeekNo = st.WeekNo
                            }).ToList()
                    }).ToList();

                return Request.CreateResponse(HttpStatusCode.OK, topics);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "Error retrieving topics", Details = ex.Message });
            }
        }

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetLectureFiles(int courseId,int checkListId)
        {
            try
            {
                var files = (from f in db.FolderContentDocument
                             join fc in db.FolderContent on f.FolderContentId equals fc.Id
                             join a in db.Allocation on f.CourseInSOSId equals a.CourseInSOSId 
                             join c in db.CourseInSOS on a.CourseInSOSId equals c.Id 
                             join s in db.Session on a.SessionId equals s.Id
                             where c.CourseId == courseId 
                             && s.Flag == true 
                             && fc.FolderCheckListId == checkListId
                             select new
                             {
                                 f.DisplayName,
                                 f.FilePath
                             }).Distinct()
                             .ToList();

                // Return the result
                return Request.CreateResponse(HttpStatusCode.OK, files);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "Error retrieving files", Details = ex.Message });
            }
        }

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage getTeacherForCommonCourse(int courseId)
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
            try
            {
                var teachers = (from a in db.Allocation
                                join c in db.CourseInSOS on a.CourseInSOSId equals c.Id
                                join ss in db.SchemeOfStudy on c.SoSId equals ss.Id
                                join p in db.Program on ss.ProgramId equals p.Id
                                join s in db.Session on a.SessionId equals s.Id
                                join course in db.Course on c.CourseId equals course.Id
                                join u in db.User on a.UserId equals u.Id
                                where course.Id == courseId
                                select new
                                {
                                    TeacherName = u.Username,
                                    SemesterNo = a.SemesterNo, 
                                    Section = a.Section ,
                                    Program = p.Id
                                }).Distinct().ToList();

                return Request.CreateResponse(HttpStatusCode.OK, teachers);
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

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetCoveredTopicsAndSubTopics(int allocationId, int sectionId)
        {
            try
            {
                var allocation = db.Allocation.FirstOrDefault(a => a.Id == allocationId && a.SectionId == sectionId);
                if (allocation == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Allocation not found for the specified section." });
                }

                var coveredTopics = (from tc in db.TopicCovered
                                     join mt in db.MasterTopic on tc.TopicId equals mt.Id
                                     where tc.AllocationId == allocationId && tc.Flag == true
                                     select new
                                     {
                                         TopicId = tc.TopicId,
                                         TopicName = mt.Name,
                                         WeekNo = tc.WeekNo
                                     }).ToList();

                var coveredSubTopics = (from stc in db.SubTopicCovered
                                        join mst in db.MasterSubTopic on stc.SubTopicId equals mst.Id
                                        where stc.AllocationId == allocationId && stc.Flag == true
                                        select new
                                        {
                                            SubTopicId = stc.SubTopicId,
                                            SubTopicName = mst.Name,
                                            WeekNo = stc.WeekNo
                                        }).ToList();

                var result = new
                {
                    CoveredTopics = coveredTopics,
                    CoveredSubTopics = coveredSubTopics
                };

                if (!coveredTopics.Any() && !coveredSubTopics.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "No covered topics or subtopics found for the specified allocation and section." });
                }

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "An error occurred while retrieving covered topics and subtopics.", Details = ex.Message });
            }
        }

            

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage TopicsSubTopicsWithFlags(int courseId)
        {

            //get the current session Id.
            int sessionId = db.Session.First(s => s.Flag == true).Id;

            // Retrieve all Allocation records with the specified CourseId
            var allocations = db.Allocation
                                .Where(a => a.CourseInSOS.Course.Id == courseId && a.SessionId == sessionId)
                                .OrderBy(a=>a.CourseInSOSId).ThenBy(a=>a.SectionId)
                                .ToList();

            if (allocations.Any())
            {
                var data = db.MasterTopic.Where(m=>m.CourseId==courseId).ToList().Select(t =>
                    new CoveredDetail
                    {
                        topic = t,
                        subtopics = db.MasterSubTopic.ToList().Where(s => s.TopicId == t.Id)
                        .Select(s=>new CoveredDetailSubtopic { subTopic=s, status=new List<FlagDetail>() }).ToList(),
                        status = new List<FlagDetail>()
                    }
                    ).ToList();
                foreach (var item in data)
                {
                    foreach (var a in allocations)
                    {  
                        var flag = false;
                        if (item.subtopics.Count() == 0)
                        {
                           flag = db.TopicCovered.FirstOrDefault(c => c.AllocationId == a.Id && c.TopicId==item.topic.Id) != null;
                            var detail = new FlagDetail {flag=flag, SectionId = a.SectionId.Value, ProgramId = a.CourseInSOS.SchemeOfStudy.ProgramId };
                            item.status.Add(detail);
                        }
                        else
                        {
                            foreach (var sub in item.subtopics)
                            {
                                flag = db.SubTopicCovered.FirstOrDefault(c => c.AllocationId == a.Id && c.SubTopicId == sub.subTopic.Id) != null;
                                var detail = new FlagDetail { flag = flag, SectionId = a.SectionId.Value , ProgramId = a.CourseInSOS.SchemeOfStudy.ProgramId };
                                sub.status.Add(detail);
                            }
                        }

                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK,data);
            }
            return Request.CreateResponse(HttpStatusCode.NoContent, "No Allocation found");
        }
        [HttpPost]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public  HttpResponseMessage UpdateTopicStatus([FromBody] TopicCovered topicCovered)
        {
            if (topicCovered == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Invalid topic data.")
                };
            }

            db.TopicCovered.Add(topicCovered);
             db.SaveChanges();

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Topic status updated successfully.")
            };

            return response;
        }

        //[HttpGet]
        //public HttpResponseMessage GetCommonlyCoveredTopics(int courseId)
        //{
        //    try
        //    {
        //        //get the current session Id.
        //        int sessionId = db.Session.First(s => s.Flag == true).Id;

        //        // Retrieve all Allocation records with the specified CourseId
        //        var allocations = db.Allocation
        //                            .Where(a => a.CourseInSOS.Course.Id == courseId && a.SessionId==sessionId)
        //                            .Select(a => a.Id)
        //                            .ToList();

        //        if (!allocations.Any())
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "No allocations found for the specified course." });
        //        }
        //        var commonlyCoveredTopics = (from tc in db.TopicCovered
        //                                     join mt in db.MasterTopic on tc.TopicId equals mt.Id
        //                                     where tc.AllocationId.HasValue
        //                                           && allocations.Contains(tc.AllocationId.Value)
        //                                           && tc.Flag == true
        //                                     group tc by new { tc.TopicId, mt.Name, tc.WeekNo } into topicGroup
        //                                     where topicGroup.Count() > 1  // Select only commonly covered topics
        //                                     select new
        //                                     {
        //                                         TopicId = topicGroup.Key.TopicId,
        //                                         TopicName = topicGroup.Key.Name,
        //                                         WeekNo = topicGroup.Key.WeekNo,
        //                                         CoveredSectionsCount = topicGroup.Count()
        //                                     }).ToList();

        //        var commonlyCoveredSubTopics = (from stc in db.SubTopicCovered
        //                                        join mst in db.MasterSubTopic on stc.SubTopicId equals mst.Id
        //                                        where stc.AllocationId.HasValue
        //                                              && allocations.Contains(stc.AllocationId.Value)
        //                                              && stc.Flag == true
        //                                        group stc by new { stc.SubTopicId, mst.Name, stc.WeekNo } into subTopicGroup
        //                                        where subTopicGroup.Count() > 1  // Select only commonly covered subtopics
        //                                        select new
        //                                        {
        //                                            SubTopicId = subTopicGroup.Key.SubTopicId,
        //                                            SubTopicName = subTopicGroup.Key.Name,
        //                                            WeekNo = subTopicGroup.Key.WeekNo,
        //                                            CoveredSectionsCount = subTopicGroup.Count()
        //                                        }).ToList();

        //        if (!commonlyCoveredTopics.Any() && !commonlyCoveredSubTopics.Any())
        //        {
        //            return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "No commonly covered topics or subtopics found across sections for this course." });
        //        }

        //        return Request.CreateResponse(HttpStatusCode.OK, new
        //        {
        //            CommonlyCoveredTopics = commonlyCoveredTopics,
        //            CommonlyCoveredSubTopics = commonlyCoveredSubTopics
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "An error occurred while retrieving commonly covered topics and subtopics.", Details = ex.Message });
        //    }
        //}







    }
}