namespace Sales_user.Models
{
    public class WorkflowResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long EntityId { get; set; }

        public static WorkflowResult Ok(long entityId, string message)
        {
            return new WorkflowResult { Success = true, EntityId = entityId, Message = message };
        }

        public static WorkflowResult Fail(string message)
        {
            return new WorkflowResult { Success = false, Message = message };
        }
    }
}
