using MACSAPI.Data;
using MACSAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MACSAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskAssignmentController : Controller
    {
        private readonly AppDbContext _dbContext;

        public TaskAssignmentController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskAssignmentGroup>>> GetAllGroups()
        {
            var groups = await _dbContext.TaskAssignmentGroups.Include(g => g.Tasks).ToListAsync();
            return Ok(groups);
        }

        [HttpGet("GetByAccountId/{accountId}")]
        public async Task<ActionResult<TaskAssignmentGroup>> GetGroupByAccountId(int accountId)
        {
            var group = await _dbContext.TaskAssignmentGroups.Include(g => g.Tasks).FirstOrDefaultAsync(t => t.AccountId == accountId);
            if (group == null)
            {
                return NotFound($"Không tìm thấy nhóm công việc với AccountId: {accountId}");
            }
            return Ok(group);
        }
        [HttpGet("GetTaskByAccountId/{accountId}")]
        public async Task<ActionResult<IEnumerable<TaskAssignment>>> GetTasksByAccountId(int accountId)
        {
            var groups = await _dbContext.TaskAssignmentGroups
                                         .Where(g => g.AccountId == accountId)
                                         .Include(g => g.Tasks)
                                         .ToListAsync();

            if (groups == null || groups.Count == 0)
            {
                return NotFound($"Không tìm thấy nhóm công việc nào với AccountId: {accountId}");
            }

            var tasks = groups.SelectMany(g => g.Tasks).ToList();

            if (tasks == null || tasks.Count == 0)
            {
                return NotFound($"Không có nhiệm vụ nào trong các nhóm với AccountId: {accountId}");
            }

            return Ok(tasks);
        }


        [HttpGet("GetByTaskId/{taskId}")]
        public async Task<ActionResult<TaskAssignment>> GetGroupByTaskId(Guid taskId)
        {
            var task = await _dbContext.TaskAssignments.FirstOrDefaultAsync(t => t.TaskId == taskId);

            if (task == null)
            {
                return NotFound($"Không tìm thấy nhóm công việc chứa TaskId: {taskId}");
            }

            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult> CreateGroup([FromBody] TaskAssignmentGroup newGroup)
        {
            if (newGroup == null)
            {
                return BadRequest("Dữ liệu nhóm công việc không hợp lệ.");
            }

                newGroup.GroupTaskId = Guid.NewGuid();
                newGroup.AssignedDate = DateTime.Now;

                foreach (var task in newGroup.Tasks)
                {
                    if (task.TaskId == Guid.Empty)
                    {
                        task.TaskId = Guid.NewGuid();
                    }
                }

                _dbContext.TaskAssignmentGroups.Add(newGroup);
            

            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGroupByAccountId), new { accountId = newGroup.AccountId }, newGroup);
        }

        [HttpPut("{accountId}/tasks/{taskId}")]
        public async Task<ActionResult> UpdateTaskStatus(int accountId, Guid taskId, [FromBody] UpdateTaskStatusRequest request)
        {
            var groups = await _dbContext.TaskAssignmentGroups
                .Include(g => g.Tasks)
                .Where(g => g.AccountId == accountId)
                .ToListAsync();

            if (groups == null || !groups.Any())
            {
                return NotFound($"Không tìm thấy nhóm công việc với AccountId: {accountId}");
            }

            var task = groups
                .SelectMany(g => g.Tasks)
                .FirstOrDefault(t => t.TaskId == taskId);

            if (task == null)
            {
                return NotFound($"Không tìm thấy công việc với TaskId: {taskId} trong nhóm AccountId: {accountId}");
            }

            task.Status = request.NewStatus;

            var groupContainingTask = groups.First(g => g.Tasks.Any(t => t.TaskId == taskId));
            groupContainingTask.UpdateBy = request.UpdatedBy;
            groupContainingTask.UpdateDate = DateTime.UtcNow;

            _dbContext.Entry(task).State = EntityState.Modified;
            _dbContext.Entry(groupContainingTask).State = EntityState.Modified;

            await _dbContext.SaveChangesAsync();

            return Ok($"Cập nhật trạng thái công việc TaskId {taskId} thành '{request.NewStatus}' thành công.");
        }


        [HttpDelete("{GroupId}")]
        public async Task<ActionResult> DeleteGroup(Guid GroupId)
        {
            var group = await _dbContext.TaskAssignmentGroups.Include(g => g.Tasks).FirstOrDefaultAsync(t => t.GroupTaskId == GroupId);
            if (group == null)
            {
                return NotFound($"Không tìm thấy nhóm công việc với AccountId: {GroupId}");
            }

            _dbContext.TaskAssignmentGroups.Remove(group);
            await _dbContext.SaveChangesAsync();

            return Ok($"Đã xóa nhóm công việc với AccountId: {GroupId}");
        }
    }
}
