
using UnityEngine;


[RequireComponent(typeof(InputReader))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))] //good practive to make sure the required components are attached to the object we are controlling, the assigned object not having a character controller, input reader or animator component will result in a runtime error

public class PlayerStateMachine : StateMachine //holds public references to components and members used in the states
{
  
    
    public Vector3 Velocity;
    public float MovementSpeed { get; private set; } = 6f;
    public float DashForce { get; private set; } = 6f;
    public float LookRotationDampFactor { get; private set; } = 10f;
    public Transform MainCamera { get; private set; }
    public InputReader InputReader { get; private set; }
    public Animator Animator { get; private set; }
    public CharacterController Controller { get; private set; }

    private void Start()
    {
        MainCamera = Camera.main.transform;

        InputReader = GetComponent<InputReader>();
        Animator = GetComponent<Animator>();
        Controller = GetComponent<CharacterController>();

        SwitchState(new PlayerMoveState(this));
    }
}

public abstract class PlayerBaseState : State
{
    float MovementSpeedMultiplier = 5.5f; //setting run speed
    protected readonly PlayerStateMachine stateMachine;

    protected PlayerBaseState(PlayerStateMachine stateMachine) // accepts the PlayerStateMachine and then assigns the reference to the stateMachine
    {
        this.stateMachine = stateMachine;
    }

    protected void CalculateMoveDirection() //calculate the direction of player movement based on the orientation of the camera and input values from InputReader.MoveComposite
    {
        Vector3 cameraForward = new(stateMachine.MainCamera.forward.x, 0, stateMachine.MainCamera.forward.z);
        Vector3 cameraRight = new(stateMachine.MainCamera.right.x, 0, stateMachine.MainCamera.right.z);

        Vector3 moveDirection = cameraForward.normalized * stateMachine.InputReader.MoveComposite.y + cameraRight.normalized * stateMachine.InputReader.MoveComposite.x;

        stateMachine.Velocity.x = moveDirection.x * stateMachine.MovementSpeed; // set the Velocity x and z values to respective values of calculated direction, multiplied by the MovementSpeed.
        stateMachine.Velocity.z = moveDirection.z * stateMachine.MovementSpeed;
    }

    protected void FaceMoveDirection() // rotate the player so it’s always facing the direction of movement
    {
        Vector3 faceDirection = new(stateMachine.Velocity.x, 0f, stateMachine.Velocity.z);

        if (faceDirection == Vector3.zero)
            return;

        stateMachine.transform.rotation = Quaternion.Slerp(stateMachine.transform.rotation, Quaternion.LookRotation(faceDirection), stateMachine.LookRotationDampFactor * Time.deltaTime);
    }

   // protected void ApplyGravity() //cause the player to be constantly pulled to the ground, keeps player grounded in move state
   // {
   //     if (stateMachine.Velocity.y > Physics.gravity.y)
   //     {
   //         stateMachine.Velocity.y += Physics.gravity.y * Time.deltaTime;
   //     }
   // }

    protected void Move() // actually move the player using its CharacterController component.
    {
        stateMachine.Controller.Move(stateMachine.Velocity * MovementSpeedMultiplier * Time.deltaTime);
    }
}

public class PlayerMoveState : PlayerBaseState
{
    private readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private readonly int MoveBlendTreeHash = Animator.StringToHash("MoveBlendTree");
    private const float AnimationDampTime = 0.1f;
    private const float CrossFadeDuration = 0.1f;

    public PlayerMoveState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
      //  stateMachine.Velocity.y = Physics.gravity.y;

        stateMachine.Animator.CrossFadeInFixedTime(MoveBlendTreeHash, CrossFadeDuration);

        stateMachine.InputReader.OnDashPerformed += SwitchToDashState;
    }

    public override void Tick()
    {
      //  if (!stateMachine.Controller.isGrounded)
      //  {
      //      stateMachine.SwitchState(new PlayerFallState(stateMachine));
      //  }

        CalculateMoveDirection();
        FaceMoveDirection();
        Move();

        stateMachine.Animator.SetFloat(MoveSpeedHash, stateMachine.InputReader.MoveComposite.sqrMagnitude > 0f ? 1f : 0f, AnimationDampTime, Time.deltaTime);
    }

    public override void Exit()
    {
        stateMachine.InputReader.OnDashPerformed -= SwitchToDashState;
    }

    private void SwitchToDashState()
    {
        stateMachine.SwitchState(new PlayerDashState(stateMachine));
    }
}

public class PlayerDashState : PlayerBaseState
{
    private readonly int DashHash = Animator.StringToHash("Dash");
    private const float CrossFadeDuration = 0.1f;
    private float DashTimeLength = 0f;

    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        
        stateMachine.Velocity = new Vector3(stateMachine.Velocity.x* stateMachine.DashForce, stateMachine.Velocity.y, stateMachine.Velocity.z* stateMachine.DashForce); //trying to increase speed in the direction of running by dash force
        stateMachine.Animator.CrossFadeInFixedTime(DashHash, CrossFadeDuration);
    }

    public override void Tick()
    {
        //ApplyGravity();
        //need to stop dash after period of frames since dash start

        //if (stateMachine.Velocity.y <= 0f)
        //{
        //    //stateMachine.SwitchState(new PlayerFallState(stateMachine));
        //    
        //}
        DashTimeLength = DashTimeLength + Time.deltaTime;
        
        if (DashTimeLength >= .05) //seeing if this will stop a dash after 5 frames, need to use time.delta time
        {
            stateMachine.SwitchState(new PlayerMoveState(stateMachine)); // want to go back to move state after dash is done
            DashTimeLength = 0;//need something to mark the time of dash start
        }

        FaceMoveDirection();
        Move();
    }

    public override void Exit() { }
}

public class PlayerFallState : PlayerBaseState
{
    private readonly int FallHash = Animator.StringToHash("Fall");
    private const float CrossFadeDuration = 0.1f;

    public PlayerFallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Velocity.y = 0f;

        stateMachine.Animator.CrossFadeInFixedTime(FallHash, CrossFadeDuration);
    }

    public override void Tick()
    {
        //ApplyGravity();
        Move();

        if (stateMachine.Controller.isGrounded)
        {
            stateMachine.SwitchState(new PlayerMoveState(stateMachine));
        }
    }

    public override void Exit() { }
}