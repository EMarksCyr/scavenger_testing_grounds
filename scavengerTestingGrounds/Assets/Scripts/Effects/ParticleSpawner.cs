using UnityEngine;
using UnityEngine.VFX;

public class ParticleSpawner : MonoBehaviour
{
    [SerializeField] VisualEffect effect;
    [SerializeField] VisualEffect _effectPrefab;
    void Start()
    {
        SpawnParticle();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SpawnParticle()
    {
        //instantiate new particle
        VisualEffect newEffect = Instantiate(_effectPrefab, transform.position, transform.rotation);

        //play the particle
        newEffect.Play();

        //destroy the particle
        Destroy(newEffect.gameObject, 1.5f); //after 1.5 sec for now, need to add in way to get lifetime of particle effect
    }
}
