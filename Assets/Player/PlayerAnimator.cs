using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DensetsuEngine.Controller{
    public class PlayerAnimator : MonoBehaviour{


        PlayerController _controller;

        // Start is called before the first frame update
        void Start()
        {
            _controller = GetComponentInParent<PlayerController>(); 

        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void UnlockAnim(){
            _controller.UnlockAnim();    
        }




    }
}
