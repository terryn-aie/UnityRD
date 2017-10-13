using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerMovementMecanim
{
    public Animator anim;

    public void Update(PlayerMovement movement)
    {
        Vector3 curVel = movement.lastVelocity;
        curVel.y = 0.0f;
        anim.SetFloat("Speed", curVel.magnitude);
    }
}
