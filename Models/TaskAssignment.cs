using System.ComponentModel.DataAnnotations;

namespace MACSAPI.Models
{
    public class TaskAssignment
    {
        [Key]
        public Guid TaskId { get; set; } 
        public string TaskName { get; set; } 
        public string Description { get; set; } 
        public DateTime DueDate { get; set; } 
        public string Status { get; set; } 
        public int Priority { get; set; } 
    }
    public class TaskAssignmentGroup
    {
        [Key]
        public Guid GroupTaskId { get; set; } 
        public int AccountId { get; set; } 
        public string AssignedTo { get; set; }
        public string AssignedBy { get; set; }
        public DateTime AssignedDate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime UpdateDate { get; set; }
        public string GroupName { get; set; }
        public List<TaskAssignment> Tasks { get; set; }

        public TaskAssignmentGroup()
        {
            Tasks = new List<TaskAssignment>();
        }
    }

    public class UpdateTaskStatusRequest
    {
        public string NewStatus { get; set; }
        public string UpdatedBy { get; set; }
    }

}
