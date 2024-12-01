﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace teacherFolderManagment.Models
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class TeacherFolderManagementEntities : DbContext
    {
        public TeacherFolderManagementEntities()
            : base("name=TeacherFolderManagementEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<C_ActivityType> C_ActivityType { get; set; }
        public virtual DbSet<Allocation> Allocation { get; set; }
        public virtual DbSet<CLO> CLO { get; set; }
        public virtual DbSet<Course> Course { get; set; }
        public virtual DbSet<CourseInSOS> CourseInSOS { get; set; }
        public virtual DbSet<Enrollment> Enrollment { get; set; }
        public virtual DbSet<Folder> Folder { get; set; }
        public virtual DbSet<FolderCheckList> FolderCheckList { get; set; }
        public virtual DbSet<FolderContent> FolderContent { get; set; }
        public virtual DbSet<FolderContentDetail> FolderContentDetail { get; set; }
        public virtual DbSet<FolderContentDetailCLO> FolderContentDetailCLO { get; set; }
        public virtual DbSet<FolderContentDetailCLOScore> FolderContentDetailCLOScore { get; set; }
        public virtual DbSet<FolderContentDocument> FolderContentDocument { get; set; }
        public virtual DbSet<FolderSubCheckList> FolderSubCheckList { get; set; }
        public virtual DbSet<LearningObjectiveContent> LearningObjectiveContent { get; set; }
        public virtual DbSet<MasterSubTopic> MasterSubTopic { get; set; }
        public virtual DbSet<MasterTopic> MasterTopic { get; set; }
        public virtual DbSet<PLO> PLO { get; set; }
        public virtual DbSet<Program> Program { get; set; }
        public virtual DbSet<Role> Role { get; set; }
        public virtual DbSet<SchemeOfStudy> SchemeOfStudy { get; set; }
        public virtual DbSet<Section> Section { get; set; }
        public virtual DbSet<Session> Session { get; set; }
        public virtual DbSet<Staff> Staff { get; set; }
        public virtual DbSet<Student> Student { get; set; }
        public virtual DbSet<SubTopic> SubTopic { get; set; }
        public virtual DbSet<SubTopicCovered> SubTopicCovered { get; set; }
        public virtual DbSet<TeacherGrader> TeacherGrader { get; set; }
        public virtual DbSet<Topic> Topic { get; set; }
        public virtual DbSet<TopicCovered> TopicCovered { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<UserRole> UserRole { get; set; }
        public virtual DbSet<TopicCLO> TopicCLO { get; set; }
    }
}