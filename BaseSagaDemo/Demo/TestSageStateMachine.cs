using BaseSagaDemo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseSagaDemo.Demo
{
    public class MyTestState : BaseState
    {

    }
    public class TestSageStateMachine : BaseStateMachine<MyTestState>
    {
        public TestSageStateMachine() : base()
        {
            
        }
    }

    public record TestStart : BaseStartSagaStateMachine<MyTestState>
    {
        public string EmpCode { get; set; } = string.Empty;
        public DateTimeOffset ActivityTimeUtc { get; set; } = DateTime.UtcNow;

    }
}
