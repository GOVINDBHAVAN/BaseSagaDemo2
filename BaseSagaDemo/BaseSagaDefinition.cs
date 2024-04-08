using MassTransit;

namespace BaseSagaDemo
{

    /// <summary>
    /// Represent Saga state machine instance, base class to encapsulate the infrastructure required to initialize the <see cref="MassTransitStateMachine{TState}"/>
    /// </summary>
    /// <typeparam name="TState">Hold state entity of the saga state machine, must be a derived class of <see cref="BaseState"/></typeparam>
    public abstract class BaseStateMachine<TState> : MassTransitStateMachine<TState>
        where TState : BaseState, new()
    {
        public BaseStateMachine()
        {
            // Here error is generating: The event property is not writable: StartProcessing
            Event(() => StartProcessing, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => ProcessingStatusRequested, x =>
            {
                x.CorrelateById(context => context.Message.CorrelationId);
                x.ReadOnly = true;
                x.OnMissingInstance(m => m.Fault());
            });

            InstanceState(x => x.CurrentState);

            Initially(When(StartProcessing)
                .Then(x =>
                {
                    x.Saga.CorrelationId = x.Message.CorrelationId;
                    OnStartProcessing(x);
                })
                .TransitionTo(Processing)
            );

            DuringAny(
                When(ProcessingStatusRequested)
                    .Respond(x =>
                    {
                        var rtn = new BaseSagaStateMachineStatusResult
                        {
                            Id = x.Saga.CorrelationId,
                            CurrentState = x.Saga.CurrentState,
                        };
                        return rtn;
                    }
                )
            );
        }

        /// <summary>
        /// Initialize Saga instance on the start of <see cref="Processing"/> method to initalize the Saga state machine
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnStartProcessing(BehaviorContext<TState, BaseStartSagaStateMachine<TState>> context)
        {
        }

        /// <summary>
        /// State represents currently it's in processing stage
        /// </summary>
        public State Processing { get; set; }

        /// <summary>
        /// Event which fires to return the current state of saga state machine
        /// </summary>
        public Event<BaseSagaStateMachineStatus> ProcessingStatusRequested { get; set; }

        /// <summary>
        /// Event which fires to actually start the saga state machine instance
        /// </summary>
        public Event<BaseStartSagaStateMachine<TState>> StartProcessing { get; set; }
    }

    #region Base Classes
    [ExcludeFromTopology]
    public abstract record SagaBaseEvent
    {
        public Guid CorrelationId { get { return Id; } set { Id = value; } }
        public Guid Id { get; set; }
    }

    /// <summary>
    /// This is base class of the state instance used in SagaStateMachineInstance <see cref="BaseStateMachine{T}"/>
    /// </summary>
    public abstract class BaseState : SagaStateMachineInstance
    {
        // Uncomment this
        //public int CurrentState { get; set; }

        // Comment this
        public string CurrentState { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
    }

    /// <summary>
    /// Base event to get the current status of saga state machine
    /// </summary>
    public record BaseSagaStateMachineStatus : SagaBaseEvent
        //where T : BaseState
    {
    }

    /// <summary>
    /// Result to retun, the current status of the Saga state machine in the response to call of <see cref="BaseSagaStateMachineStatus{T}"/> event to the caller
    /// </summary>
    public record BaseSagaStateMachineStatusResult
    {
        public Guid Id { get; init; }

        //Uncomment this
        //public int CurrentState { get; set; }

        // comment this
        public string CurrentState { get; set; } = string.Empty;
    }

    /// <summary>
    /// Base event to start the saga state machine instance
    /// </summary>
    public record BaseStartSagaStateMachine<T> : SagaBaseEvent
        where T : BaseState
    {
        //public string EmpCode { get; set; } = string.Empty;
        //public DateTimeOffset ActivityTimeUtc { get; set; } = DateTime.UtcNow;
    }

    #endregion

}
