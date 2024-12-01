using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using teacherFolderManagment.Models;
namespace TeacherFolderManagementSystem.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class SettingsController : ApiController
    {
        //a function will accept session id and will generate subcheck list for assignments(4), Quizes(8),Samples and Solutions
        TeacherFolderManagementEntities db = new TeacherFolderManagementEntities();

        [HttpGet]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage ConfigureSubCheckList(int sessionId)
        {
            try
            {

                var assignmentId = db.FolderCheckList.FirstOrDefault(x => x.Name.Equals("Assignment")).Id;
                var quizId = db.FolderCheckList.FirstOrDefault(x => x.Name.Equals("Quizes")).Id;
                var sampleId = db.FolderCheckList.FirstOrDefault(x => x.Name.Equals("Samples")).Id;
                var solutionId = db.FolderCheckList.FirstOrDefault(x => x.Name.Equals("Solutions")).Id;


                var anyAssignment = db.FolderSubCheckList.Where(x => x.SessionId == sessionId && x.FolderCheckListId == assignmentId);
                var anyQUiz = db.FolderSubCheckList.Where(x => x.SessionId == sessionId && x.FolderCheckListId == quizId);
                var anySample = db.FolderSubCheckList.Where(x => x.SessionId == sessionId && x.FolderCheckListId == sampleId);
                var anySolution = db.FolderSubCheckList.Where(x => x.SessionId == sessionId && x.FolderCheckListId == solutionId);


                if (!anyAssignment.Any())
                {
                    //if no checklist for default assignments.
                    for (int number = 1; number <= 4; number++)
                    {
                        var assignmentSubChecklistDetail = new FolderSubCheckList { FolderCheckListId = assignmentId, Name = $"Assignment-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(assignmentSubChecklistDetail);
                        db.SaveChanges();

                        //add solution against each assignment
                        var assignmentSolutionSubChecklistDetail = new FolderSubCheckList { ParentId = assignmentSubChecklistDetail.Id, FolderCheckListId = solutionId, Name = $"Assignment-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(assignmentSolutionSubChecklistDetail);
                        db.SaveChanges();


                        //add sample against each assignment
                        //For Best
                        var assignmentSample1SubChecklistDetail = new FolderSubCheckList { ParentId = assignmentSubChecklistDetail.Id, FolderCheckListId = sampleId, Name = $"Best-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(assignmentSample1SubChecklistDetail);

                        //For Best
                        var assignmentSample2SubChecklistDetail = new FolderSubCheckList { ParentId = assignmentSubChecklistDetail.Id, FolderCheckListId = sampleId, Name = $"Average-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(assignmentSample2SubChecklistDetail);

                        //For Best
                        var assignmentSample3SubChecklistDetail = new FolderSubCheckList { ParentId = assignmentSubChecklistDetail.Id, FolderCheckListId = sampleId, Name = $"Worst-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(assignmentSample3SubChecklistDetail);
                        db.SaveChanges();

                    }
                }

                if (!anyQUiz.Any())
                {
                    //if no checklist for default assignments.
                    for (int number = 1; number <= 8; number++)
                    {
                        var quizSubChecklistDetail = new FolderSubCheckList { FolderCheckListId = quizId, Name = $"Quiz-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(quizSubChecklistDetail);
                        db.SaveChanges();

                        //add solution against each quiz
                        var quizSolutionSubChecklistDetail = new FolderSubCheckList { ParentId = quizSubChecklistDetail.Id, FolderCheckListId = solutionId, Name = $"Quiz-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(quizSolutionSubChecklistDetail);
                        db.SaveChanges();


                        //add sample against each quiz
                        //For Best
                        var quizSample1SubChecklistDetail = new FolderSubCheckList { ParentId = quizSubChecklistDetail.Id, FolderCheckListId = sampleId, Name = $"Best-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(quizSample1SubChecklistDetail);

                        //For Best
                        var quizSample2SubChecklistDetail = new FolderSubCheckList { ParentId = quizSubChecklistDetail.Id, FolderCheckListId = sampleId, Name = $"Average-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(quizSample2SubChecklistDetail);

                        //For Best
                        var quizSample3SubChecklistDetail = new FolderSubCheckList { ParentId = quizSubChecklistDetail.Id, FolderCheckListId = sampleId, Name = $"Worst-{number}", SessionId = sessionId };
                        db.FolderSubCheckList.Add(quizSample3SubChecklistDetail);
                        db.SaveChanges();

                    }
                }


                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}