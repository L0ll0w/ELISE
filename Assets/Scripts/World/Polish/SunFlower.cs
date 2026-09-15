using Gamelogic.Extensions;
using UnityEngine;

public class SunFlower : MonoBehaviour
{

    private GameObject _player;

    [SerializeField] private float rotationOffset = 0f;

    [SerializeField] private float smoothSpeed = 3f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion targetRotation;

        if (_player != null)
        {
            Vector3 dirToPlayer = _player.transform.position - this.transform.position;
            dirToPlayer.y = 0;

            if (dirToPlayer != Vector3.zero)
            {
                Quaternion targetLook = Quaternion.LookRotation(dirToPlayer);
                targetRotation = targetLook * Quaternion.Euler(-90f, rotationOffset, 0f);
            }
            else
            {
                targetRotation = transform.rotation;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);

        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _player = other.gameObject;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _player = null;
        }


    }
}
