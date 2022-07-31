using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApeFree.DataStore.Demo
{
    public interface IStudent
    {
        string Address { get; set; }
        string ClassName { get; set; }
        DateTime DateOfBirth { get; set; }
        string Description { get; set; }
        long Id { get; set; }
        bool IsYoungPioneer { get; set; }
        string Name { get; set; }
    }

    /// <summary>
    /// 学生（测试实体类）
    /// </summary>
    public class Student : IStudent
    {
        public long Id { get; set; } = 2022030511;
        public string Name { get; set; } = "张三";
        public DateTime DateOfBirth { get; set; } = new DateTime(2013, 6, 1);
        public string ClassName { get; set; } = "甲班";
        public string Description { get; set; } = "普通学生。";
        public bool IsYoungPioneer { get; set; } = true;
        public string Address { get; set; } = "A区B道100号";
    }
}
