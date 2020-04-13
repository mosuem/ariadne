using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionParticleSystem : MonoBehaviour {
    public Material partMaterial;
    public Material trailMaterial;
    public IEnumerator SetParticleSystem (MPath path) {
        var pathObject = path.gameObject;
        var partsystem = pathObject.GetComponent<ParticleSystem> ();
        if (partsystem == null) {
            partsystem = pathObject.AddComponent<ParticleSystem> ();
        }
        var renderer = partsystem.GetComponent<ParticleSystemRenderer> ();
        renderer.maxParticleSize = 0.01f;
        renderer.minParticleSize = 0.01f;
        renderer.material = partMaterial;
        var main = partsystem.main;
        main.maxParticles = 1;
        main.startSpeed = 0;
        main.startLifetime = 1000;
        main.playOnAwake = true;
        var emission = partsystem.emission;
        emission.rateOverTime = 0.5f;
        renderer.sortingOrder = path.sortingOrder + 1;
        var enumerator = AddParticleSystem (path);

        var trails = partsystem.trails;
        trails.enabled = true;
        trails.lifetime = 0.0003f;
        trailMaterial.SetColor ("_Color", new Color (1f, 1f, 1f, 0.3f));
        renderer.trailMaterial = trailMaterial;

        AnimationCurve animationCurve = new AnimationCurve ();
        animationCurve.AddKey (0f, Statics.lineThickness * 0.8f);
        animationCurve.AddKey (1f, 0f);
        ParticleSystem.MinMaxCurve minMaxCurve = new ParticleSystem.MinMaxCurve (1f, animationCurve);
        minMaxCurve.curve = animationCurve;
        trails.widthOverTrail = minMaxCurve;

        StartCoroutine (enumerator);
        return enumerator;
    }

    public IEnumerator AddParticleSystem (MPath path) {
        float timeSum = 0f;
        bool isOk = true;
        var nextPosition = 0;
        while (path != null && path.Count > 0 && isOk) {
            isOk = false;
            try {
                var m_currentParticleEffect = path.gameObject.GetComponent<ParticleSystem> ();
                var numParticles = m_currentParticleEffect.particleCount;
                ParticleSystem.Particle[] ParticleList = new ParticleSystem.Particle[numParticles];
                m_currentParticleEffect.GetParticles (ParticleList);

                for (int i = 0; i < numParticles; ++i) {
                    if (path != null) {
                        timeSum += Time.deltaTime;
                        ParticleList[i].position = path.GetPosition (nextPosition);
                        nextPosition = nextPosition + 1 < path.Count ? nextPosition + 1 : 0;
                    }
                }

                m_currentParticleEffect.SetParticles (ParticleList, numParticles);
                isOk = true;
            } catch (MissingReferenceException ex) {
                Debug.Log (ex);
                yield break;
            }
            yield return null;
        }
        yield break;
    }
}