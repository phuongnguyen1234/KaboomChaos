﻿using UnityEngine;

namespace Player
{
	//Advanced walker controller script;
	//This controller is used as a basis for other controller types ('SidescrollerController');
	//Custom movement input can be implemented by creating a new script that inherits 'AdvancedWalkerController' and overriding the 'CalculateMovementDirection' function;
	public class AdvancedWalkerController : Controller {

		//References to attached components;
		protected Transform tr;
		protected Mover mover;
		protected CharacterInput characterInput;
		protected CeilingDetector ceilingDetector;

        //Jump key variables;
        protected bool jumpInputIsLocked = false;
        protected bool jumpKeyWasPressed = false;
		bool jumpKeyWasLetGo = false;
		bool jumpKeyIsPressed = false;

		//Movement speed;
		[Header("Movement")]
		[Tooltip("Tốc độ di chuyển của nhân vật trên mặt đất.")]
		[SerializeField] private float _movementSpeed = 7f;

		//How fast the controller can change direction while in the air;
		//Higher values result in more air control;
		[Tooltip("Tốc độ thay đổi hướng khi ở trên không. Giá trị cao hơn cho phép điều khiển trên không tốt hơn.")]
		[SerializeField] private float _airControlRate = 2f;

		//Jump speed;
		[Header("Jumping")]
		[Tooltip("Lực nhảy ban đầu của nhân vật.")]
		[SerializeField] protected float _jumpSpeed = 10f;

		[Tooltip("Khoảng thời gian ngắn (tính bằng giây) sau khi rời khỏi mặt đất mà người chơi vẫn có thể nhảy. Giúp tăng 'game feel' khi nhảy ở rìa.")]
		[SerializeField] private float _coyoteTimeDuration = 0.1f;

		//Jump duration variables;
		[Tooltip("Thời gian tối đa có thể giữ nút nhảy để đạt chiều cao tối đa. Cho phép thay đổi chiều cao nhảy.")]
		[SerializeField] protected float _jumpDuration = 0.2f;
		float currentJumpStartTime = 0f;

		private float _groundContactLostTime;

		//'AirFriction' determines how fast the controller loses its momentum while in the air;
		//'GroundFriction' is used instead, if the controller is grounded;
		[Header("Physics")]
		[Tooltip("'AirFriction' xác định tốc độ nhân vật mất đà khi ở trên không.")]
		[SerializeField] private float _airFriction = 0.5f;
		[Tooltip("'GroundFriction' được sử dụng thay thế khi nhân vật ở trên mặt đất. Giá trị cao giúp nhân vật dừng lại nhanh hơn.")]
		[SerializeField] private float _groundFriction = 100f;
		[Tooltip("Vật liệu vật lý được coi là trơn trượt. Khi đứng trên bề mặt có vật liệu này, ma sát sẽ giảm đáng kể.")]
		[SerializeField] private PhysicsMaterial _slipperyPhysicsMaterial;
		[Tooltip("Ma sát sẽ được sử dụng khi đứng trên bề mặt trơn trượt.")]
		[SerializeField] private float _slipperyFriction = 1f;
		[Tooltip("Tốc độ nhân vật có thể thay đổi hướng khi trượt trên băng. Giá trị thấp hơn tạo cảm giác trơn trượt hơn.")]
		[SerializeField] private float _iceControlRate = 5f;

		//Current momentum;
		protected Vector3 momentum = Vector3.zero;

		//Momentum of the ground object the character is standing on;
		protected Vector3 groundMomentum = Vector3.zero;

		//Amount of downward gravity;
		[Tooltip("Lực hấp dẫn hướng xuống tác dụng lên nhân vật.")]
		[SerializeField] private float _gravity = 30f;
		[Tooltip("How fast the character will slide down steep slopes.")]
		[SerializeField] private float _slideGravity = 5f;
		
		//Acceptable slope angle limit;
		[Tooltip("Góc dốc tối đa nhân vật có thể đi lên mà không bị trượt.")]
		[SerializeField] private float _slopeLimit = 80f;

		[Tooltip("Whether to calculate and apply momentum relative to the controller's transform.")]
		[SerializeField] protected bool _useLocalMomentum = false;

		//Enum describing basic controller states; 
		protected enum ControllerState
		{
			Grounded,
			Sliding,
			Falling,
			Rising,
			Jumping,
			Climbing // Thêm trạng thái Climbing
		}
		
		protected ControllerState currentControllerState = ControllerState.Falling;

		[Tooltip("Optional camera transform used for calculating movement direction. If assigned, character movement will take camera view into account.")]
		[SerializeField] protected Transform cameraTransform;

		//Saved velocity from last frame;
		Vector3 savedVelocity = Vector3.zero;

		//Saved horizontal movement velocity from last frame;
		Vector3 savedMovementVelocity = Vector3.zero;

		// Cờ để theo dõi trạng thái trên băng của frame trước, dùng để phát hiện chuyển đổi.
		private bool _wasOnIce;

		//Getters;
		public float movementSpeed => _movementSpeed;
		public Transform CameraTransform => cameraTransform;
		
		//Get references to all necessary components;
		void Awake () {
			mover = GetComponent<Mover>();
			tr = transform;
			characterInput = GetComponent<CharacterInput>();
			ceilingDetector = GetComponent<CeilingDetector>();

			if(characterInput == null)
				Debug.LogWarning("No character input script has been attached to this gameobject", this.gameObject);

			Setup();
		}

		//This function is called right after Awake(); It can be overridden by inheriting scripts;
		protected virtual void Setup()
		{
		}

		void Update()
		{
			HandleJumpKeyInput();
		}

        //Handle jump booleans for later use in FixedUpdate;
        void HandleJumpKeyInput()
        {
            bool _newJumpKeyPressedState = IsJumpKeyPressed();

            if (jumpKeyIsPressed == false && _newJumpKeyPressedState == true)
                jumpKeyWasPressed = true;

            if (jumpKeyIsPressed == true && _newJumpKeyPressedState == false)
            {
                jumpKeyWasLetGo = true;
                jumpInputIsLocked = false;
            }

            jumpKeyIsPressed = _newJumpKeyPressedState;
        }

        /// <summary>
        /// Cập nhật controller trong vòng lặp vật lý.
        /// Được đánh dấu là 'protected virtual' để các lớp con có thể ghi đè và thêm logic mà không làm mất đi chức năng gốc.
        /// </summary>
        protected virtual void FixedUpdate()
		{
			ControllerUpdate();
		}

		//Update controller;
		//This function must be called every fixed update, in order for the controller to work correctly;
		void ControllerUpdate()
		{
			//Check if mover is grounded;
			mover.CheckForGround();

			//If mover is grounded, calculate ground momentum;
			//This is the velocity of the ground object at the point of contact;
			if(mover.IsGrounded())
			{
				//Get ground collider;
				Collider _groundCollider = mover.GetGroundCollider();

				if(_groundCollider != null)
				{
					//If ground object has a rigidbody, get its velocity;
					Rigidbody _groundRigidbody = _groundCollider.attachedRigidbody;
					if(_groundRigidbody != null)
					{
						groundMomentum = _groundRigidbody.GetPointVelocity(mover.GetGroundPoint());
					}
					else
						groundMomentum = Vector3.zero;
				}
				else
					groundMomentum = Vector3.zero;
			}
			else
				groundMomentum = Vector3.zero;

			//Determine controller state;
			currentControllerState = DetermineControllerState();

			//Apply friction and gravity to 'momentum';
			HandleMomentum();

			//Check if the player has initiated a jump;
			HandleJumping();

			//Calculate movement velocity;
			Vector3 _velocity = Vector3.zero;

			// CẢI TIẾN: Xử lý di chuyển trên băng
			bool isOnIce = false;
            if (currentControllerState == ControllerState.Grounded)
            {
                Collider groundCollider = mover.GetGroundCollider();
                if (groundCollider != null && _slipperyPhysicsMaterial != null && groundCollider.sharedMaterial == _slipperyPhysicsMaterial)
                {
                    isOnIce = true;
                }
            }

			// CẢI TIẾN: Nếu vừa bước lên băng từ mặt đất thường, chuyển vận tốc hiện tại thành momentum.
			// Điều này bảo toàn tốc độ chạy đà của người chơi lên bề mặt băng, tạo cảm giác trượt tự nhiên.
			if (isOnIce && !_wasOnIce)
			{
				// savedMovementVelocity chứa vận tốc dựa trên input từ frame trước.
				momentum += savedMovementVelocity;
			}

			// Nếu ở trên mặt đất thông thường (không trơn trượt), áp dụng vận tốc di chuyển trực tiếp.
			// Nếu ở trên không hoặc trên băng, việc di chuyển sẽ được xử lý thông qua 'momentum' trong HandleMomentum().
			if(currentControllerState == ControllerState.Grounded && !isOnIce)
				_velocity = CalculateMovementVelocity();
			
			//If local momentum is used, transform momentum into world space first;
			Vector3 _worldMomentum = momentum;
			if(_useLocalMomentum)
				_worldMomentum = tr.localToWorldMatrix * momentum;

			//Add current momentum to velocity;
			_velocity += _worldMomentum;
			
			//If player is grounded or sliding on a slope, extend mover's sensor range;
			//This enables the player to walk up/down stairs and slopes without losing ground contact;
			mover.SetExtendSensorRange(IsGrounded());

			//Set mover velocity;		
			mover.SetVelocity(_velocity);

			//Store velocity for next frame;
			savedVelocity = _velocity;
		
			//Save controller movement velocity;
			savedMovementVelocity = CalculateMovementVelocity();

			// Lưu lại trạng thái trên băng cho frame tiếp theo;
			_wasOnIce = isOnIce;

			//Reset jump key booleans;
			jumpKeyWasLetGo = false;
			jumpKeyWasPressed = false;

			//Reset ceiling detector, if one is attached to this gameobject;
			if(ceilingDetector != null)
				ceilingDetector.ResetFlags();
		}

		//Calculate and return movement direction based on player input;
		//This function can be overridden by inheriting scripts to implement different player controls;
		protected virtual Vector3 CalculateMovementDirection()
		{
			//If no character input script is attached to this object, return;
			if(characterInput == null)
				return Vector3.zero;

			Vector3 _velocity = Vector3.zero;

			//If no camera transform has been assigned, use the character's transform axes to calculate the movement direction;
			if(cameraTransform == null)
			{
				_velocity += tr.right * characterInput.GetHorizontalMovementInput();
				_velocity += tr.forward * characterInput.GetVerticalMovementInput();
			}
			else
			{
				//If a camera transform has been assigned, use the assigned transform's axes for movement direction;
				//Project movement direction so movement stays parallel to the ground;
				_velocity += Vector3.ProjectOnPlane(cameraTransform.right, tr.up).normalized * characterInput.GetHorizontalMovementInput();
				_velocity += Vector3.ProjectOnPlane(cameraTransform.forward, tr.up).normalized * characterInput.GetVerticalMovementInput();
			}

			//If necessary, clamp movement vector to magnitude of 1f;
			if(_velocity.magnitude > 1f)
				_velocity.Normalize();

			return _velocity;
		}

		//Calculate and return movement velocity based on player input, controller state, ground normal [...];
		protected virtual Vector3 CalculateMovementVelocity()
		{
			//Calculate (normalized) movement direction;
			Vector3 _velocity = CalculateMovementDirection();

			//Multiply (normalized) velocity with movement speed;
			_velocity *= movementSpeed;

			return _velocity;
		}

		//Returns 'true' if the player presses the jump key;
		protected virtual bool IsJumpKeyPressed()
		{
			//If no character input script is attached to this object, return;
			if(characterInput == null)
				return false;

			return characterInput.IsJumpKeyPressed();
		}

		//Determine current controller state based on current momentum and whether the controller is grounded (or not);
		//Handle state transitions;
		protected virtual ControllerState DetermineControllerState()
		{
			//Check if vertical momentum is pointing upwards;
			bool _isRising = IsRisingOrFalling() && (VectorMath.GetDotProduct(GetMomentum(), tr.up) > 0f);
			//Check if controller is sliding;
			bool _isSliding = mover.IsGrounded() && IsGroundTooSteep();
			
			//Grounded;
			if(currentControllerState == ControllerState.Grounded)
			{
				if(_isRising){
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if(!mover.IsGrounded()){
					OnGroundContactLost();
					return ControllerState.Falling;
				}
				if(_isSliding){
					OnGroundContactLost();
					return ControllerState.Sliding;
				}
				return ControllerState.Grounded;
			}

			//Falling;
			if(currentControllerState == ControllerState.Falling)
			{
				if(_isRising){
					return ControllerState.Rising;
				}
				if(mover.IsGrounded() && !_isSliding){
					OnGroundContactRegained();
					return ControllerState.Grounded;
				}
				if(_isSliding){
					return ControllerState.Sliding;
				}
				return ControllerState.Falling;
			}
			
			//Sliding;
			if(currentControllerState == ControllerState.Sliding)
			{	
				if(_isRising){
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if(!mover.IsGrounded()){
					OnGroundContactLost();
					return ControllerState.Falling;
				}
				if(mover.IsGrounded() && !_isSliding){
					OnGroundContactRegained();
					return ControllerState.Grounded;
				}
				return ControllerState.Sliding;
			}

			//Rising;
			if(currentControllerState == ControllerState.Rising)
			{
				if(!_isRising){
					if(mover.IsGrounded() && !_isSliding){
						OnGroundContactRegained();
						return ControllerState.Grounded;
					}
					if(_isSliding){
						return ControllerState.Sliding;
					}
					if(!mover.IsGrounded()){
						return ControllerState.Falling;
					}
				}

				//If a ceiling detector has been attached to this gameobject, check for ceiling hits;
				if(ceilingDetector != null)
				{
					if(ceilingDetector.HitCeiling())
					{
						OnCeilingContact();
						return ControllerState.Falling;
					}
				}
				return ControllerState.Rising;
			}

			//Jumping;
			if(currentControllerState == ControllerState.Jumping)
			{
				//Check for jump timeout;
				if((Time.time - currentJumpStartTime) > _jumpDuration)
					return ControllerState.Rising;

				//Check if jump key was let go;
				if(jumpKeyWasLetGo)
					return ControllerState.Rising;

				//If a ceiling detector has been attached to this gameobject, check for ceiling hits;
				if(ceilingDetector != null)
				{
					if(ceilingDetector.HitCeiling())
					{
						OnCeilingContact();
						return ControllerState.Falling;
					}
				}
				return ControllerState.Jumping;
			}
			
			return ControllerState.Falling;
		}

		/// <summary>
		/// Kiểm tra xem người chơi có thực hiện nhảy không.
		/// Bao gồm logic "coyote time" để cho phép nhảy một khoảng thời gian ngắn sau khi rời khỏi mặt đất.
		/// </summary>
		private void HandleJumping()
		{
			// Điều kiện để có thể nhảy: đang ở trên mặt đất, HOẶC vừa mới rời khỏi mặt đất trong khoảng thời gian cho phép (coyote time).
			bool isGrounded = currentControllerState == ControllerState.Grounded;
			bool isCoyoteTimeActive = _groundContactLostTime > 0f && Time.time < _groundContactLostTime + _coyoteTimeDuration;

			if ((jumpKeyIsPressed || jumpKeyWasPressed) && !jumpInputIsLocked)
			{
				if (isGrounded || isCoyoteTimeActive)
				{
					// Nếu đang trên mặt đất, gọi OnGroundContactLost() để tính toán momentum cho cú nhảy.
					// Nếu đang trong coyote time, hàm này đã được gọi khi rời khỏi mặt đất.
					if (isGrounded)
					{
						OnGroundContactLost();
					}

					OnJumpStart();
					currentControllerState = ControllerState.Jumping;

					// Vô hiệu hóa coyote time để ngăn nhảy hai lần trên không.
					_groundContactLostTime = 0f;
				}
			}
		}
        //Apply friction to both vertical and horizontal momentum based on 'friction' and 'gravity';
		//Handle movement in the air;
        //Handle sliding down steep slopes;
        protected virtual void HandleMomentum()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(_useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;

			Vector3 _verticalMomentum = Vector3.zero;
			Vector3 _horizontalMomentum = Vector3.zero;

			//Split momentum into vertical and horizontal components;
			if(momentum != Vector3.zero)
			{
				_verticalMomentum = VectorMath.ExtractDotVector(momentum, tr.up);
				_horizontalMomentum = momentum - _verticalMomentum;
			}

			//Add gravity to vertical momentum;
			_verticalMomentum -= tr.up * _gravity * Time.deltaTime;

			//Remove any downward force if the controller is grounded;
			if(currentControllerState == ControllerState.Grounded && VectorMath.GetDotProduct(_verticalMomentum, tr.up) < 0f)
				_verticalMomentum = Vector3.zero;

			// CẢI TIẾN: Kiểm tra xem có đang ở trên băng không
			bool isOnIce = false;
            if (currentControllerState == ControllerState.Grounded)
            {
                Collider groundCollider = mover.GetGroundCollider();
                if (groundCollider != null && _slipperyPhysicsMaterial != null && groundCollider.sharedMaterial == _slipperyPhysicsMaterial)
                {
                    isOnIce = true;
                }
            }

			//Manipulate momentum to steer controller in the air (if controller is not grounded or sliding);
			if(!IsGrounded())
			{
				Vector3 _movementVelocity = CalculateMovementVelocity();

				//If controller has received additional momentum from somewhere else;
				if(_horizontalMomentum.magnitude > _movementSpeed)
				{
					//Prevent unwanted accumulation of speed in the direction of the current momentum;
					if(VectorMath.GetDotProduct(_movementVelocity, _horizontalMomentum.normalized) > 0f)
						_movementVelocity = VectorMath.RemoveDotVector(_movementVelocity, _horizontalMomentum.normalized);
					
					//Lower air control slightly with a multiplier to add some 'weight' to any momentum applied to the controller;
					float _airControlMultiplier = 0.25f;
					_horizontalMomentum += _movementVelocity * Time.deltaTime * _airControlRate * _airControlMultiplier;
				}
				//If controller has not received additional momentum;
				else
				{
					//Clamp _horizontal velocity to prevent accumulation of speed;
					_horizontalMomentum += _movementVelocity * Time.deltaTime * _airControlRate;
					_horizontalMomentum = Vector3.ClampMagnitude(_horizontalMomentum, _movementSpeed);
				}
			}
			// CẢI TIẾN: Nếu đang ở trên băng, áp dụng input như một lực đẩy thay vì vận tốc tức thời.
			else if (isOnIce)
			{
				Vector3 _movementVelocity = CalculateMovementVelocity();

				// Di chuyển momentum hiện tại về phía vận tốc mong muốn với một tốc độ giới hạn (iceControlRate).
				// Điều này tạo ra cảm giác trơn trượt, người chơi không thể đổi hướng ngay lập tức.
				_horizontalMomentum = Vector3.MoveTowards(_horizontalMomentum, _movementVelocity, _iceControlRate * Time.deltaTime);
			}

			//Steer controller on slopes;
			if(currentControllerState == ControllerState.Sliding)
			{
				//Calculate vector pointing away from slope;
				Vector3 _pointDownVector = Vector3.ProjectOnPlane(mover.GetGroundNormal(), tr.up).normalized;

				//Calculate movement velocity;
				Vector3 _slopeMovementVelocity = CalculateMovementVelocity();
				//Remove all velocity that is pointing up the slope;
				_slopeMovementVelocity = VectorMath.RemoveDotVector(_slopeMovementVelocity, _pointDownVector);

				//Add movement velocity to momentum;
				_horizontalMomentum += _slopeMovementVelocity * Time.fixedDeltaTime;
			}

			//Apply friction to horizontal momentum based on whether the controller is grounded;
			if(currentControllerState == ControllerState.Grounded)
			{
				// Mặc định sử dụng ma sát mặt đất thông thường.
				float currentFriction = _groundFriction;

				// CẢI TIẾN: Tái sử dụng cờ 'isOnIce' đã kiểm tra ở trên.
				if (isOnIce)
				{
					currentFriction = _slipperyFriction;
				}

				Vector3 _horizontalGroundMomentum = VectorMath.RemoveDotVector(groundMomentum, tr.up);
				_horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, currentFriction, Time.deltaTime, _horizontalGroundMomentum);
			}
			else
				_horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, _airFriction, Time.deltaTime, Vector3.zero); 

			//Add horizontal and vertical momentum back together;
			momentum = _horizontalMomentum + _verticalMomentum;

			//Additional momentum calculations for sliding;
			if(currentControllerState == ControllerState.Sliding)
			{
				//Project the current momentum onto the current ground normal if the controller is sliding down a slope;
				momentum = Vector3.ProjectOnPlane(momentum, mover.GetGroundNormal());

				//Remove any upwards momentum when sliding;
				if(VectorMath.GetDotProduct(momentum, tr.up) > 0f)
					momentum = VectorMath.RemoveDotVector(momentum, tr.up);

				//Apply additional slide gravity;
				Vector3 _slideDirection = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal()).normalized;
				momentum += _slideDirection * _slideGravity * Time.deltaTime;
			}
			
			//If controller is jumping, override vertical velocity with jumpSpeed;
			if(currentControllerState == ControllerState.Jumping)
			{
				momentum = VectorMath.RemoveDotVector(momentum, tr.up);
				momentum += tr.up * _jumpSpeed;
			}

			if(_useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//Events;

		//This function is called when the player has initiated a jump;
		protected virtual void OnJumpStart()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(_useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Add jump force to momentum;
			momentum += tr.up * _jumpSpeed;

			//Set jump start time;
			currentJumpStartTime = Time.time;

            //Lock jump input until jump key is released again;
            jumpInputIsLocked = true;

            //Call event;
            if (OnJump != null)
				OnJump(momentum);

			if(_useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//This function is called when the controller has lost ground contact, i.e. is either falling or rising, or generally in the air;
		protected virtual void OnGroundContactLost()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(_useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Get current movement velocity;
			Vector3 _velocity = GetMovementVelocity();

			//Check if the controller has both momentum and a current movement velocity;
			if(_velocity.sqrMagnitude >= 0f && momentum.sqrMagnitude > 0f)
			{
				//Project momentum onto movement direction;
				Vector3 _projectedMomentum = Vector3.Project(momentum, _velocity.normalized);
				//Calculate dot product to determine whether momentum and movement are aligned;
				float _dot = VectorMath.GetDotProduct(_projectedMomentum.normalized, _velocity.normalized);

				//If current momentum is already pointing in the same direction as movement velocity,
				//Don't add further momentum (or limit movement velocity) to prevent unwanted speed accumulation;
				if(_projectedMomentum.sqrMagnitude >= _velocity.sqrMagnitude && _dot > 0f)
					_velocity = Vector3.zero;
				else if(_dot > 0f)
					_velocity -= _projectedMomentum;	
			}

			//Add movement velocity to momentum;
			momentum += _velocity;

			// Ghi lại thời điểm mất tiếp xúc với mặt đất để kích hoạt coyote time.
			_groundContactLostTime = Time.time;

			if(_useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//This function is called when the controller has landed on a surface after being in the air;
		protected virtual void OnGroundContactRegained()
		{
			// Reset lại bộ đếm coyote time khi tiếp đất.
			_groundContactLostTime = 0f;

			//Call 'OnLand' event;
			if(OnLand != null)
			{
				Vector3 _collisionVelocity = momentum;
				//If local momentum is used, transform momentum into world coordinates first;
				if(_useLocalMomentum)
					_collisionVelocity = tr.localToWorldMatrix * _collisionVelocity;

				OnLand(_collisionVelocity);
			}
				
		}

		//This function is called when the controller has collided with a ceiling while jumping or moving upwards;
		protected virtual void OnCeilingContact()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(_useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Remove all vertical parts of momentum;
			momentum = VectorMath.RemoveDotVector(momentum, tr.up);

			if(_useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//Helper functions;

		//Returns 'true' if vertical momentum is above a small threshold;
		private bool IsRisingOrFalling()
		{
			//Calculate current vertical momentum;
			Vector3 _verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), tr.up);

			//Setup threshold to check against;
			//For most applications, a value of '0.001f' is recommended;
			float _limit = 0.001f;

			//Return true if vertical momentum is above '_limit';
			return(_verticalMomentum.magnitude > _limit);
		}

		//Returns true if angle between controller and ground normal is too big (> slope limit), i.e. ground is too steep;
		private bool IsGroundTooSteep()
		{
			if(!mover.IsGrounded())
				return true;

			return (Vector3.Angle(mover.GetGroundNormal(), tr.up) > _slopeLimit);
		}

		//Getters;

		//Get last frame's velocity;
		public override Vector3 GetVelocity ()
		{
			return savedVelocity;
		}

		//Get last frame's movement velocity (momentum is ignored);
		public override Vector3 GetMovementVelocity()
		{
			return savedMovementVelocity;
		}

		//Get current momentum;
		public Vector3 GetMomentum()
		{
			Vector3 _worldMomentum = momentum;
			if(_useLocalMomentum)
				_worldMomentum = tr.localToWorldMatrix * momentum;

			return _worldMomentum;
		}

		//Returns 'true' if controller is grounded (or sliding down a slope);
		public override bool IsGrounded()
		{
			return(currentControllerState == ControllerState.Grounded || currentControllerState == ControllerState.Sliding);
		}

		//Returns 'true' if controller is sliding;
		public bool IsSliding()
		{
			return(currentControllerState == ControllerState.Sliding);
		}

		//Add momentum to controller;
		public void AddMomentum (Vector3 _momentum)
		{
			if(_useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			momentum += _momentum;	

			if(_useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//Set controller momentum directly;
		public void SetMomentum(Vector3 _newMomentum)
		{
			if(_useLocalMomentum)
				momentum = tr.worldToLocalMatrix * _newMomentum;
			else
				momentum = _newMomentum;
		}
	}
}
