using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class enemigo : MonoBehaviour
{
    private int vidaEnemigo;

    public Rigidbody enemyRb;
    public GameObject player;
    //public GameManager gameManager;


    private void Start()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = GameObject.Find("Player");
        vidaEnemigo = 5;
        //gameManager = GameObject.Find("GameManager").GetComponent<GameManager
    }

    void Update()
    {
        if (player != null)
        {
            transform.LookAt(player.transform);
        }

        if (vidaEnemigo < 1)
        {
            EliminarEnemigo();
        }
    }

    private void FixedUpdate()
    {
        SeguirJugador(5);
    }

    private void SeguirJugador(float speed)
    {
        enemyRb.AddForce((player.transform.position - transform.position).normalized * speed * 10 * Time.deltaTime);
    }

    private void EliminarEnemigo()
    {
        Object.FindFirstObjectByType<spawnmanager>().EnemyDefeated();
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("hitbox"))
        {
            vidaEnemigo -= 1;
        }
    }
}
