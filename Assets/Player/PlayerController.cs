using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DensetsuEngine.Controller{
    public class PlayerController : MonoBehaviour{

        // player Stats 
        [SerializeField] private int hitPoints = 50;
        [SerializeField] private float moveSpeed = 4.0f;

        [Header("Player Abilities")]
        [SerializeField] private bool hasDashAbility = false;


        [Header("Player SFXs")]
        [SerializeField] private AudioClip swdAttackSfx;
        [SerializeField] private AudioClip dashSfx;
        [SerializeField] private AudioClip hurtSfx;
        [SerializeField] private AudioClip deathSfx;

        private Rigidbody2D _rb;
        private Animator _anim;
        private AudioSource _audio;

        private PlayerInputActions _input;

        // move varables
        private Vector2 _moveInput = Vector2.zero;
        private Vector2 _moveForce = Vector2.zero;


        private bool _animLocked = false;
        private bool _canMove = true;
        private Vector2 _facing = new Vector2(1,1);


        //Knockback varables [debug testing -- should be moved to an interface]
        private Vector2 _knockbackDir = Vector2.zero;
        private float _knockbackTime = 0.0f;

        private bool _isAttacking = false;
        private bool _isDead = false;
        private bool _isInteracting = false;

        //dash ability var
        private bool _isDashing = false;
        private Vector2 _dashDir = Vector2.zero;
        private float _dashSpeed = 18.0f;
        private float _dashTime = 0.0f;
        private float _startDashTime = 0.075f;
        private float _dashcooldownTime = 0.0f;
        private float _startCoolDownTime = 0.8f;

        //interact vars
        //private IInteractable _interactable; // game obj the player can interact with

        // carry vars
        [SerializeField] GameObject carryPos;
        private bool _isCarrying = false;



        private bool debug_inPlayerMenu = false;


        #region On Enable/Disable Functions
        private void OnEnable() {
        
            _input = new PlayerInputActions();
            _input.Player.Enable();

            _input.Player.Move.performed += SetMoveInput;
            _input.Player.Move.canceled += SetMoveInput;
            //_input.Player.Interact.performed += OnInteract;
            _input.Player.Attack.performed += OnAttack;
            _input.Player.Dash.performed += OnDash;

            //debug testing controls
            _input.Player.DebugStart.performed += DebugPressedStart;
           

        }
        private void OnDisable() {
        
            _input.Player.Move.performed -= SetMoveInput;
            _input.Player.Move.canceled -= SetMoveInput;
            //_input.Player.Interact.performed -= OnInteract;
            _input.Player.Attack.performed -= OnAttack;
            _input.Player.Dash.performed-= OnDash;

            //debug testing controls
            _input.Player.DebugStart.performed -= DebugPressedStart;
            
            _input.Player.Disable();
    
        }
        #endregion

        #region Game Loop Functions
        private void Awake() {
            _rb = this.GetComponent<Rigidbody2D>();
            _anim = this.GetComponentInChildren<Animator>();
            _audio = this.GetComponentInChildren<AudioSource>();

            _anim.SetFloat("FaceDir_X", _facing.x);
            _anim.SetFloat("FaceDir_Y", _facing.y);

            _dashTime = _startDashTime;

        }

        private void FixedUpdate() {
            
            if (!_isDead && hitPoints <= 0 && _knockbackDir == Vector2.zero){ 
                OnDead();
            }

            
   
            MoveUpdate();
            DashUpdate();
            UpdateAnimator();
        }
        #endregion

        #region Move Functions
        private void SetMoveInput(InputAction.CallbackContext context){
            if (_isInteracting) return;
           

            _moveInput = context.ReadValue<Vector2>();

             if (debug_inPlayerMenu) _moveInput = Vector2.zero;


            //clamp input to 1, 0, or -1
            float deadZone = 0.4f;
            if (_moveInput.x > deadZone) _moveInput.x = 1;
            if (_moveInput.y > deadZone) _moveInput.y = 1;
            if (_moveInput.x < -deadZone) _moveInput.x = -1;
            if (_moveInput.y < -deadZone) _moveInput.y = -1;
            if (Mathf.Abs(_moveInput.x) != 1) _moveInput.x = 0;
            if (Mathf.Abs(_moveInput.y) != 1) _moveInput.y = 0;

            //debug TEST: clamp to 4dir iso movement
            if (_moveInput == new Vector2(-1, 0)) _moveInput = new Vector2(-1, -1);
            if (_moveInput == new Vector2(1, 0)) _moveInput = new Vector2(1, 1);
            if (_moveInput == new Vector2(0, 1)) _moveInput = new Vector2(-1, 1);
            if (_moveInput == new Vector2(0, -1)) _moveInput = new Vector2(1, -1);

            if(_moveInput != Vector2.zero)_facing = _moveInput; //update facing in dir of movement

            //correct angle for diagonal movement in Isometric enviroment
            if (_moveInput.x != 0 && _moveInput.y != 0){
                _moveInput.y *= 0.5f;
            }
            _moveInput = _moveInput.normalized;

        }

        private bool isMovingIntoWall(Vector2 dir){

            
            RaycastHit2D hit = Physics2D.Raycast(this.transform.position, dir, 0.21f, LayerMask.GetMask("system_wall", "wall","object")); //To-Do: Move mask to a varible
            Debug.DrawLine(this.transform.position, (Vector2)this.transform.position + dir);

            if (hit){
                //Debug.Log("hit wall");
                return true; 
            
            }
          

            return false;
        }

        private void MoveUpdate(){

            if (_rb == null) {
                Debug.LogWarning("GameObject does not have a ridgidbody 2D attached");
                return;           
            }

            if(_animLocked){ 
                _rb.velocity = Vector2.zero;
                return;
            } 

             if (isMovingIntoWall(_moveInput)) _moveInput = Vector2.zero; // check if moving into wall

            //set the move Force
            if (_knockbackDir != Vector2.zero){

                _moveForce = _knockbackDir; 
                _knockbackTime -= Time.deltaTime;
                if (_knockbackTime <= 0.0f){ 
                    _knockbackTime = 0.0f;
                    _knockbackDir = Vector2.zero;
                }
            }else{ 
                _moveForce = _moveInput * moveSpeed;
            }

            _rb.velocity = _moveForce; //set move force to the obj velocity this frame;
            //CheckForInteractable(); // after we move check if we are able to inbteract with anything 
        }
        #endregion


        #region Attack Functions
        private void OnAttack(InputAction.CallbackContext context){
            //Debug.Log("R1 Attack");
            _isAttacking = true;
        }
        #endregion

        public void OnCarryStart(Transform obj){
            obj.SetParent(carryPos.transform);
            obj.localPosition = Vector2.zero;
            _isCarrying = true;
        }


        #region Dash Functions
        private void OnDash(InputAction.CallbackContext context){
            if (!hasDashAbility || _dashcooldownTime > 0) return;
            
            Debug.Log("Player Dashed");
            if (!_isDashing && _moveInput != Vector2.zero){
                //start the dash if not dashing
                _audio.PlayOneShot(dashSfx);
                GetComponentInChildren<SpriteRenderer>().color = Color.black; // DEBUG -- this is a temp way to visisuall show the player dashing
                _isDashing = true;
                _dashDir = _moveInput.normalized;
                _animLocked = true;
            }
        }
        private void DashUpdate(){ 
            
            if (_dashcooldownTime > 0){ 
                _dashcooldownTime -= Time.deltaTime;
            }
            
            if (!_isDashing) return;

            _rb.velocity = _dashDir * _dashSpeed;   // dash this frame
            _dashTime -= Time.deltaTime;            //decrease timer 

            if (_dashTime <= 0.0f){ 
                GetComponentInChildren<SpriteRenderer>().color = Color.white; // DEBUG -- this is a temp way to visisuall show the player dashing
                _isDashing= false;
                _animLocked =false;
                _dashTime = _startDashTime;
                _dashcooldownTime = _startCoolDownTime;
            }


        }
        #endregion

        #region Dead Functions
        private void OnDead(){
             hitPoints = 0;
            _isDead = true;
            _audio.PlayOneShot(deathSfx);
            _anim.Play("Death Blend Tree");
            _animLocked = true;
        }
        #endregion

        #region Animator Functions 
        private void UpdateAnimator(){

            if (_knockbackDir != Vector2.zero){ 
                if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("OnHit Blend Tree")) _anim.Play("OnHit Blend Tree");
                return;
            }

            if (_isAttacking){ 
                if (!_animLocked) _audio.PlayOneShot(swdAttackSfx); //play the sfx only if not currently attacking
                _anim.Play("Attack Blend Tree");
                _isAttacking = false;
                _animLocked = true;
                
            }
  
            if (!_animLocked && _canMove){ 
                _anim.SetFloat("FaceDir_X", _facing.x);
                _anim.SetFloat("FaceDir_Y", _facing.y);

                if (_moveInput == Vector2.zero){ 
                    if (_isCarrying){ 
                        _anim.Play("Idle Carry Blend Tree");
                    }else{ 
                        _anim.Play("Idle Blend Tree");
                    }
                    
                }else{ 
                    if (!_anim.GetCurrentAnimatorStateInfo(0).IsName("Walk Blend Tree")) _anim.Play("Walk Blend Tree");
                }
                
            
            }
            
        
        }

        public void UnlockAnim(){
            _animLocked = false;
        }
        #endregion

        #region Knockback Functions
        public void OnKnockback(Vector2 dir){
            _audio.PlayOneShot(hurtSfx);
            _knockbackDir = dir;
            _knockbackTime = 0.15f;
            hitPoints -= 10;
            _animLocked = false;
        }
        #endregion
    
    

        public void DebugPressedStart(InputAction.CallbackContext context) {
            debug_inPlayerMenu = !debug_inPlayerMenu;
        }


    }
}
