using System;

namespace HW_info
{
    public class ChangeLog
    {
        public int Id { get; set; }
        public string MacAddress { get; set; }
        public string ComputerName { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
    }
}
