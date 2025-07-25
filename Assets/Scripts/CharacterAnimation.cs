using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterAnimation : MonoBehaviour
{
    public CharacterSet characterSet;
    public float frameRate = 10f; // Frame rate for the animation
    public ParticleSystem runEffect;
    public RawImage boardImage;
    public RawImage characterImage;
    public int currentFrame = 0;
    private Coroutine walkingCoroutine;

    void Start()
    {
        if(this.characterImage == null) this.characterImage = GetComponent<RawImage>();
        this.setIdling();
        if(this.boardImage != null)
        {
            this.boardImage.texture = characterSet.boardTexture;
        }
    }

    void setParticleLayer(int layerOrder)
    {
        if (this.runEffect != null)
        {
            this.runEffect.GetComponent<ParticleSystemRenderer>().sortingOrder = layerOrder;
        }
    }

    public void PlayWalking(int layerOrder)
    {
        // If the walking coroutine is already running, do nothing
        if (this.walkingCoroutine != null) return;

        this.setParticleLayer(layerOrder);

        // Start the walking animation coroutine
        this.walkingCoroutine = StartCoroutine(this.walkingAnimation());
    }
    private IEnumerator walkingAnimation()
    {
        int currentFrame = 0;

        while (true) // Loop indefinitely while walking
        {
            if (this.characterImage != null)
            {
                this.characterImage.texture = this.characterSet.walkingAnimationTextures[currentFrame];
            }

            currentFrame = (currentFrame + 1) % this.characterSet.walkingAnimationTextures.Length;
            this.playParticle();
            yield return new WaitForSeconds(1f / this.frameRate); // Wait for the frame duration
        }
    }

    // Call this method to switch animation sets
    public void setIdling()
    {
        // Stop the walking coroutine if it's running
        if (this.walkingCoroutine != null)
        {
            if (this.runEffect != null) this.runEffect.Stop();
            StopCoroutine(this.walkingCoroutine);
            this.walkingCoroutine = null; // Clear the reference
        }

        if (this.characterImage != null)
            this.characterImage.texture = this.characterSet.idlingTexture;
    }

    public void playParticle()
    {
        if (this.runEffect != null && !this.runEffect.isPlaying)
        {
            this.runEffect.Play();
        }
    }
}
