using System.Collections.Generic;
using System.Linq;
using teacherFolderManagment.Models;

namespace TeacherFolderManagement.Controllers
{
    public class CoveredDetail
    {
        public MasterTopic topic { get; set; }
        public List<CoveredDetailSubtopic> subtopics { get; set; }
        public List<FlagDetail> status { get; set; }
    }

    public class FlagDetail
    {
        public int SectionId { get; set; }
        public int ProgramId { get; set; }
        public bool flag { get; set; }
    }

    public class CoveredDetailSubtopic
    {
        public MasterSubTopic subTopic { get; set; }
        public List<FlagDetail> status { get; set; }
    }
}