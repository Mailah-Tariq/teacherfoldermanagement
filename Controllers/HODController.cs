using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using teacherFolderManagment.Models;

namespace teacherFolderManagment.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class HODController : ApiController
    {

        TeacherFolderManagementEntities db = new TeacherFolderManagementEntities();
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
        public HttpResponseMessage GetCoursesList()
        {
            db.Configuration.ProxyCreationEnabled = false;
            try
            {
                var CourseLists = (from alloc in db.Allocation
                                   join csos in db.CourseInSOS on alloc.CourseInSOSId equals csos.Id
                                   join course in db.Course on csos.CourseId equals course.Id
                                   join session in db.Session on alloc.SessionId equals session.Id
                                   where session.Flag == true
                                   select course)
                                   .Distinct()
                                   .OrderBy(c => c.Title)
                                   .ToList();

                if (CourseLists == null || !CourseLists.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No folder checklists found.");
                }

                return Request.CreateResponse(HttpStatusCode.OK, CourseLists);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "An error occurred while retrieving folder checklists: " + ex.Message);
            }
        }


        //[HttpGet]
        //public HttpResponseMessage GetAllCoursesOfSpecificSession()
        //{
        //    try
        //    {
        //        var courses = db.Allocation
        //            .Where(a => a.se)
        //            .Select(a => new
        //            {
        //                CourseId = a.CourseInSOS.Course.Id,
        //                CourseCode = a.CourseInSOS.Course.CourseCode,
        //                CourseTitle = a.CourseInSOS.Course.Title,
        //                SectionTitle = a.Section.Title,
        //                SemesterNo = a.SemesterNo,
        //                SessionId = a.SessionId
        //            })
        //            .Distinct().ToList();

        //        return Request.CreateResponse(HttpStatusCode.OK, courses);

        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage TeachersForCourse(int courseId)
        {
            db.Configuration.LazyLoadingEnabled = false;
            try
            {
                var teachers = db.CourseInSOS
                    .Where(c => c.CourseId == courseId)
                    .Join(db.Allocation, s => s.Id, a => a.CourseInSOSId, (s, a) => new { Allocation = a, SoSId = s.SoSId })
                    .Join(db.User, a => a.Allocation.UserId, u => u.Id, (a, u) => new { User = u, SoSId = a.SoSId })
                    .Join(
                        db.SchemeOfStudy.Where(sos => sos.ActiveStatus==true), // Filter out inactive schemes
                        au => au.SoSId,
                        sos => sos.Id,
                        (au, sos) => new { au.User, ProgramId = sos.ProgramId }
                    )
                    .Join(db.Program, us => us.ProgramId, p => p.Id, (us, p) => new
                    {
                        Id = us.User.Id,
                        Name = us.User.Name,
                        ProgramId = p.Id,
                        ProgramShortName = p.ShortName,
                        ProgramTitle = p.Title,
                        us.User.Dob,
                        us.User.Username,
                        us.User.Password,
                        us.User.ImagePath,
                        us.User.IsActive,
                    })
                    .Distinct()
                    .OrderBy(u => u.Name)
                    .ToList();

                return Request.CreateResponse(HttpStatusCode.OK, teachers);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage AssignMasterFolderToTeacher(int userId, int programId, int courseId)
        {
            try
            {
                var program = db.Program.FirstOrDefault(p => p.Id == programId);
                if (program == null)
                    throw new Exception("Program Name missing/invalid");

                int sosId = db.SchemeOfStudy.First(s => s.ProgramId == program.Id && s.ActiveStatus == true).Id;

                var allocInfo = db.CourseInSOS.Where(c => c.CourseId == courseId && c.SoSId == sosId)
                    .Join(db.Allocation.Where(a => a.UserId == userId), c => c.Id, a => a.CourseInSOSId, (c, a) => a.Id).ToList();

                if (allocInfo.Any() == false)
                    throw new Exception("Allocation missing/invalid");


                var folder = db.Folder.FirstOrDefault(f => allocInfo.Contains(f.AllocationId));
                if (folder == null)
                    db.Folder.Add(new Folder { AllocationId = allocInfo[0], Type = "M" });
                else
                    folder.Type = "M";

                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Allocated");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

       // Retrieve of master Folder
       [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetAssignedFoldersForTeacher(int programId, int courseId)
        {
            try
            {
                var sosId = db.SchemeOfStudy
                              .Where(s => s.ProgramId == programId && s.ActiveStatus == true)
                              .Select(s => s.Id)
                              .FirstOrDefault();

                if (sosId == 0)
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Invalid Program or Scheme of Study not found" });

                var allocatedFolders = (from c in db.CourseInSOS
                                        join s in db.SchemeOfStudy on c.SoSId equals s.Id
                                        join p in db.Program on s.ProgramId equals p.Id
                                        join a in db.Allocation on c.Id equals a.CourseInSOSId into allocations
                                        from a in allocations.DefaultIfEmpty()
                                        join f in db.Folder on a.Id equals f.AllocationId into folders
                                        from f in folders.DefaultIfEmpty()
                                        join u in db.User on a.UserId equals u.Id into users
                                        from u in users.DefaultIfEmpty()
                                        where c.CourseId == courseId
                                              && c.SoSId == sosId
                                        select new
                                        {
                                            FolderId = f != null ? f.Id : 0,
                                            FolderType = f != null ? f.Type : "None",
                                            AllocationId = a != null ? a.Id : 0,
                                            TeacherId = u != null ? u.Id : 0,
                                            TeacherName = u != null ? u.Name : "No Teacher",
                                            ProgramName = p.ShortName
                                        }).ToList();

                var uniqueFolders = allocatedFolders
                    .Where(folder => folder.FolderId != 0)
                    .GroupBy(folder => new { folder.TeacherId, folder.ProgramName })
                    .Select(g => g.FirstOrDefault())
                    .ToList();

                if (!uniqueFolders.Any())
                    return Request.CreateResponse(HttpStatusCode.OK, new List<object>());

                return Request.CreateResponse(HttpStatusCode.OK, uniqueFolders);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "Error fetching folders.", Details = ex.Message });
            }
        }

        [HttpDelete]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage DeleteAssignedFolders(int allocationId)
        {
            try
            {
                var delFolder = db.Folder.Where(f => f.AllocationId == allocationId).ToList(); ;
                if (!delFolder.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { Message = "Folder not found" });
                }
                db.Folder.RemoveRange(delFolder);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Folder deleted successfully" });
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { Message = "Error deleting folder.", Details = ex.Message });
            }
        }
 [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage GetProgramsByCourseCode(string courseCode)
        {
            try
            {
                // Retrieve the course based on the course code
                var course = db.Course.FirstOrDefault(c => c.CourseCode == courseCode);
                if (course == null)
                    throw new Exception("Course Code missing/invalid");

                // Retrieve all CourseInSOS entries for the course
                var courseInSOSList = db.CourseInSOS.Where(c => c.CourseId == course.Id).ToList();
                if (!courseInSOSList.Any())
                    throw new Exception("No CourseInSOS found for this course.");

                // Get all Scheme of Study IDs from the CourseInSOS entriess
                var sosIds = courseInSOSList.Select(c => c.SoSId).Distinct();

                // Retrieve all Schemes of Study linked to the CourseInSOS
                var sosList = db.SchemeOfStudy.Where(s => sosIds.Contains(s.Id) && s.ActiveStatus == true).ToList();
                if (!sosList.Any())
                    throw new Exception("No Scheme of Study found for this course.");

                // Get all Program IDs from the Schemes of Study
                var programIds = sosList.Select(s => s.ProgramId).Distinct();

                // Retrieve all Programs linked to the Schemes of Study
                var programs = db.Program.Where(p => programIds.Contains(p.Id)).ToList();
                if (!programs.Any())
                    throw new Exception("No Programs found for this Scheme of Study.");

                // Return the list of program titles and short names
                var programTitles = programs.Select(p => new { p.Title, p.ShortName }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, programTitles);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}
