using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace teacherFolderManagment.Models
{
    public class NewAllocationSchema
    {
        public string CourseCode { get; set; }
        public string Teacher { get; set; }
        public string SemesterNo { get; set; }
        public string Section { get; set; }
        public string Session { get; set; }
        public string Program { get; set; }
    }

}