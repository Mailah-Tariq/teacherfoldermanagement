using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using teacherFolderManagment.Models;
namespace teacherFolderManagment.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class FolderController : ApiController
    {
        TeacherFolderManagementEntities db = new TeacherFolderManagementEntities();
        [HttpPost]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage UploadFolderContent()
        {
            try
            {

                db.Configuration.ProxyCreationEnabled = false;
                var httpRequest = HttpContext.Current.Request;

                int? courseInSOSId = null;
                int? folderId = null;

                if (httpRequest["FolderId"] == null)
                {
                    if (httpRequest["CourseInSOSId"] == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "FolderId/CourseInSOSId is missing");
                    else
                    {
                        courseInSOSId = Convert.ToInt32(httpRequest["CourseInSOSId"]);
                    }
                }
                else
                {
                    folderId = Convert.ToInt32(httpRequest["FolderId"]);

                }

                if (httpRequest["FolderCheckListId"] == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Folder Checklist Id is missing");
                }

                if (httpRequest["DisplayName"] == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "DisplayName is missing");
                }

                var folderChecklistId = Convert.ToInt32(httpRequest["FolderCheckListId"]);

                int? folderSubChecklistId = null;
                //check if "FolderSubCheckListId" exists. Optional
                if (httpRequest["FolderSubCheckListId"] != null)
                {
                    folderSubChecklistId = Convert.ToInt32(httpRequest["FolderSubCheckListId"]);
                }

                var displayNames = httpRequest["DisplayName"];
                var names = displayNames.Split(new char[] { ',' });

                //folder content is uploaded here.
                var contents = new FolderContent
                {
                    FolderId = folderId,
                    FolderCheckListId = folderChecklistId,
                    FolderSubCheckListId = folderSubChecklistId
                };
                db.FolderContent.Add(contents);
                db.SaveChanges();

                //now upload as many as files are requested.
                for (int i = 0; i < httpRequest.Files.Count; i++)
                {

                    var ext = httpRequest.Files[i].FileName;
                    var fileName = DateTime.Now.ToFileTime() + "." + ext;

                    var contentDocuement = new FolderContentDocument
                    {
                        FolderContentId = contents.Id,
                        DisplayName = names[i],
                        FilePath = fileName,
                        CourseInSOSId = courseInSOSId
                    };

                    var filePathName = HttpContext.Current.Server.MapPath($"~/FolderData/{fileName}");

                    httpRequest.Files[0].SaveAs(filePathName);

                    db.FolderContentDocument.Add(contentDocuement);

                }
                db.SaveChanges();
                //if Checklist type is Quiz, Assignment, Exam
                if (folderChecklistId == 1 || folderChecklistId == 3 || folderChecklistId == 5)
                {
                    var noOfQuestions = Convert.ToInt32(httpRequest["NoOfQuestions"]);
                    for (int q = 1; q <= noOfQuestions; q++)
                    {
                        var detail = new FolderContentDetail
                        {
                            FolderContentId = contents.Id,
                            QuestionNo = q,
                            Marks = 0
                        };
                        db.FolderContentDetail.Add(detail);
                    }
                    db.SaveChanges();

                }

                return Request.CreateResponse(HttpStatusCode.OK, contents);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, ex);
            }
        }

        /// <summary>
        /// Add details againsts the specific content like Quiz/Assignment/Paper marks against CLO/Questons, 
        /// </summary>
        /// <param name="detail"></param>
        /// <returns></returns>
        [HttpPost]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        public HttpResponseMessage UploadFolderContentDetail(dynamic detail)
        {
            try
            {
                int count = detail.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = detail[i];
                    int id = Convert.ToInt32(item["Id"]);
                    int marks = Convert.ToInt32(item["Marks"]);
                    var ccount = item["CLOIds"].Count;

                    var contentData = db.FolderContentDetail.FirstOrDefault(d => d.Id == id);
                    if (contentData != null)
                    {
                        contentData.Marks = marks;

                        for (int c = 0; c < ccount; c++)
                        {
                            var cloId = Convert.ToInt32(item["CLOIds"][c]);
                            db.FolderContentDetailCLO.Add(new FolderContentDetailCLO { CLOId = cloId, FolderContentDetailId = id });
                        }
                    }

                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}
