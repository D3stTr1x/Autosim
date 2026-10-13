using UnityEngine;

public class EngineSound : MonoBehaviour
{
    [SerializeField] private AudioSource idleSource;
    [SerializeField] private AudioSource lowSource;
    [SerializeField] private AudioSource midSource;
    [SerializeField] private AudioSource highSource;
    
    // Новый источник — звук отсечки (короткий, НЕ зацикленный!)
    [SerializeField] private AudioSource otsechkaSource;

    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField, Range(0.01f, 1f)] private float fadeSpeed = 0.12f;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.45f;

    [SerializeField] private float maxSpeed = 240f;         // скорость начала отсечки
    [SerializeField] private float lowThreshold = 60f;
    [SerializeField] private float midThreshold = 130f;
    [SerializeField] private float highThreshold = 180f;


    [SerializeField] private float minPitch = 0.7f;
    [SerializeField] private float maxPitch = 2.5f;


    [SerializeField, Range(0.3f, 1f)] private float limiterVolume = 0.8f;
    private float limiterTimer = 0f;
    private bool wasInLimiterLastFrame = false;

    private float currentSpeedKmh;

    private void Start()
    {
        SetLoopAndPlay(idleSource);
        SetLoopAndPlay(lowSource);
        SetLoopAndPlay(midSource);
        SetLoopAndPlay(highSource);

        // otsechkaSource НЕ зацикливаем и НЕ играем в Start!
        if (otsechkaSource != null)
        {
            otsechkaSource.loop = false;
            otsechkaSource.playOnAwake = false;
        }

        lowSource.volume = midSource.volume = highSource.volume = 0f;
    }

    private void SetLoopAndPlay(AudioSource source)
    {
        if (source != null)
        {
            source.loop = true;
            source.Play();
        }
    }

    private void Update()
    {
        if (targetRigidbody == null) return;

        currentSpeedKmh = targetRigidbody.linearVelocity.magnitude * 3.6f;
        UpdateEngineSound();
    }

    private void UpdateEngineSound()
    {
        float speed = currentSpeedKmh;
        float normalizedSpeed = Mathf.Clamp01(speed / maxSpeed);
        bool inLimiter = speed >= maxSpeed;

        // === Вес слоёв (как было) ===
        float idleWeight = 0f, lowWeight = 0f, midWeight = 0f, highWeight = 0f;

        if (speed <= 1f)
            idleWeight = 1f;
        else if (speed < lowThreshold)
        {
            float t = (speed - 1f) / (lowThreshold - 1f);
            idleWeight = 1f - t;
            lowWeight = t;
        }
        else if (speed < midThreshold)
        {
            float t = (speed - lowThreshold) / (midThreshold - lowThreshold);
            lowWeight = 1f - t;
            midWeight = t;
        }
        else if (speed < highThreshold)
        {
            float t = (speed - midThreshold) / (highThreshold - midThreshold);
            midWeight = 1f - t;
            highWeight = t;
        }
        else
        {
            highWeight = 1f;
        }

        // Если в отсечке — резко приглушаем все основные слои
        float limiterDamp = inLimiter ? 0.25f : 1f; // основной звук становится тише

        idleSource.volume = Mathf.Lerp(idleSource.volume, idleWeight, fadeSpeed) * masterVolume * limiterDamp;
        lowSource.volume  = Mathf.Lerp(lowSource.volume,  lowWeight,  fadeSpeed) * masterVolume * limiterDamp;
        midSource.volume  = Mathf.Lerp(midSource.volume,  midWeight,  fadeSpeed) * masterVolume * limiterDamp;
        highSource.volume = Mathf.Lerp(highSource.volume, highWeight, fadeSpeed) * masterVolume * limiterDamp;

        // === Pitch ===
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Min(normalizedSpeed, 0.98f)); // не доходим до 1.0

        lowSource.pitch  = Mathf.Lerp(lowSource.pitch,  targetPitch * 0.95f, fadeSpeed * 0.7f);
        midSource.pitch  = Mathf.Lerp(midSource.pitch,  targetPitch * 1.05f, fadeSpeed * 0.7f);
        highSource.pitch = Mathf.Lerp(highSource.pitch, targetPitch * 1.15f, fadeSpeed * 0.7f);
        idleSource.pitch = Mathf.Lerp(0.85f, 1.15f, speed / 40f);

        // === ОТСЕЧКА ===
        if (inLimiter)  // speed >= maxSpeed
        {
            // Приглушаем основной двигатель (как будто топливо/зажигание отрубается)
            float engineDamp = 0.15f; // почти глохнет
            idleSource.volume = Mathf.Lerp(idleSource.volume, idleWeight * engineDamp, fadeSpeed * 2f) * masterVolume;
            lowSource.volume = Mathf.Lerp(lowSource.volume, lowWeight * engineDamp, fadeSpeed * 2f) * masterVolume;
            midSource.volume = Mathf.Lerp(midSource.volume, midWeight * engineDamp, fadeSpeed * 2f) * masterVolume;
            highSource.volume = Mathf.Lerp(highSource.volume, highWeight * engineDamp, fadeSpeed * 2f) * masterVolume;

            // Сама отсечка — короткие резкие "удары" с рандомизацией
            limiterTimer -= Time.deltaTime;
            if (limiterTimer <= 0f)
            {
                if (otsechkaSource != null)
                {
                    otsechkaSource.volume = Random.Range(0.7f, 1.0f) * masterVolume;
                    otsechkaSource.pitch = Random.Range(0.92f, 1.08f); // лёгкий "дрожащий" эффект

                    // ВАЖНО: используем PlayOneShot с самим клипом, а не AudioSource.Play()
                    otsechkaSource.PlayOneShot(otsechkaSource.clip);
                }

                // Рандомизируем интервал — от 0.10 до 0.22 сек (имитация 6500–7500 об/мин)
                limiterTimer = Random.Range(0.10f, 0.22f);
            }
        }
        else
        {
            // Когда вышли из отсечки — плавно возвращаем громкость двигателя
            idleSource.volume = Mathf.Lerp(idleSource.volume, idleWeight, fadeSpeed * 3f) * masterVolume;
            lowSource.volume = Mathf.Lerp(lowSource.volume, lowWeight, fadeSpeed * 3f) * masterVolume;
            midSource.volume = Mathf.Lerp(midSource.volume, midWeight, fadeSpeed * 3f) * masterVolume;
            highSource.volume = Mathf.Lerp(highSource.volume, highWeight, fadeSpeed * 3f) * masterVolume;
        }

        // Сброс таймера при выходе из отсечки
        if (!inLimiter && wasInLimiterLastFrame)
            limiterTimer = 0f;

        wasInLimiterLastFrame = inLimiter;
    }
}