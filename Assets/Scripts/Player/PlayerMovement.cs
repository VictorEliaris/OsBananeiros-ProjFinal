using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
	Rigidbody rigid;
	[SerializeField]
	Animator anim;
	GameObject CameraDummy;
	[SerializeField]
	Pause pause;
	[SerializeField]
	TransitionScript TS;
	
	Vector3 Movement, VelocityWOY;//direção do movimento
	public float moveForce, dragForce;//força/velocidade do movimento
	public float speedLimiterMult, speedLimiterPlus;//limitam a velocidade máxima
	[SerializeField]
	bool grab;//se o jogador está agarrando algo
	
	bool mayJump;//se o jogador pode pular
	public float jumpRaycastSize;//quão perto do chão o jogador tem que estar pra pular
	public float jumpForce;//força do pulo
	float jumpTimer;//timer da força do pulo
	public float jumpDuration;//duração do pulo, afeta a distância máxima segurando space
	
	[SerializeField]
	bool dying, dead;
	public float deathTime;
	//se o jogador não consegue se mover
	public bool stunned;
	public float stunTime;
	
	//estados do jogador
	public enum PlayerState
	{
		Idle,
		Walk,
		Jump,
		Dead,
	}
	//estado atual do jogador
	public PlayerState State;
	
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }
	
	public void SetCameraDummy(GameObject dummy)
	{
		CameraDummy = dummy;
	}

    void Update()
    {
		if(!dying && !stunned)
		{
			//direção do movimento
			Movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
			//faz a direção do movimento ser baseada na rotação da camera
			if(CameraDummy) Movement = CameraDummy.transform.TransformDirection(Movement);
			
			//faz o jogador rotacionar para a direção do movimento
			// !grab para ele não rotacionar enquanto agarra/empurra algo
			if(Movement.magnitude > 0 && !grab)
			{
				transform.forward = Vector3.Slerp(transform.forward, Movement, Time.deltaTime * 10);
			}
			
			//checa se o jogador pode pular
			mayJump = Physics.Raycast(transform.position, Vector3.down, jumpRaycastSize);
			anim.SetBool("Grounded", mayJump);
			Debug.DrawLine(transform.position, transform.position + (Vector3.down * jumpRaycastSize), Color.white);
			
			//pulo, não acontece quando o jogo ta pausado
			if(!pause.gamePaused && Input.GetButtonDown("Jump"))
			{
				if(mayJump)
				{
					anim.SetTrigger("Jumping");
					ChangeState(PlayerState.Jump);
				}
			}
		}
		//timer de stun, deixa o jogador se mover após 1 segundo
		else if(stunned)
		{
			stunTime += Time.deltaTime;
			if(stunTime >= 1)
			{
				stunned = false;
				stunTime = 0;
			}
		}
    }
	
	void FixedUpdate()
	{
		if(!dying && !stunned)
		{
			float velocity = rigid.velocity.magnitude;
			anim.SetFloat("Velocity", velocity);
			
			//movimento e limite de velocidade
			rigid.AddForce((Movement * moveForce) / (velocity * speedLimiterMult + speedLimiterPlus));
			
			//tira o Y do velocity
			VelocityWOY = new Vector3(rigid.velocity.x, 0, rigid.velocity.z);
			//estabiliza o movimento
			rigid.AddForce(-VelocityWOY * dragForce);
		}
		else if(dying)
			rigid.velocity = new Vector3(0, rigid.velocity.y, 0);
	}
	
	//função para facilitar a mudança de estados
	public void ChangeState(PlayerState thisState)
	{
		State = thisState;
		StartCoroutine(State.ToString());
	}
	
	//jogador parado
	IEnumerator Idle()
	{
		while (State == PlayerState.Idle)
		{
			yield return new WaitForFixedUpdate();
			
			if(rigid.velocity.magnitude > 0.1f)
			{
				ChangeState(PlayerState.Walk);
			}
		}
	}
	
	//jogador andando
	IEnumerator Walk()
	{
		while (State == PlayerState.Walk)
		{
			yield return new WaitForFixedUpdate();
			if(rigid.velocity.magnitude < 0.1f)
			{
				ChangeState(PlayerState.Idle);
			}
		}
	}
	
	//jogador pulando
	IEnumerator Jump()
	{
		jumpTimer = jumpDuration;
		
		while(State == PlayerState.Jump)
		{
			yield return new WaitForFixedUpdate();
			
			rigid.AddForce(Vector3.up * jumpForce * jumpTimer);
			
			jumpTimer -= Time.fixedDeltaTime;
			if(jumpTimer <= 0)
			{
				ChangeState(PlayerState.Idle);
			}
		}
	}
	
	//jogador jogado para trás, acontece quando toma dano
	public void Knockback(float force)
	{
		stunned = true;
		
		//knockback, para trás e um pouco para cima relativo ao jogador
		rigid.AddRelativeForce(0, force/2, -force);
	}
	
	//jogador morto
	IEnumerator Dead()
	{
		dying = true;
		yield return new WaitForSeconds(deathTime);
		TS.Transition(false, SceneManager.GetActiveScene().name);
	}
}
