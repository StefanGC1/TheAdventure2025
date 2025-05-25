using Silk.NET.SDL;

namespace TheAdventure.Models;

public class TemporaryGameObject : RenderableGameObject
{
    public double Ttl { get; init; }
    public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= Ttl;

    private DateTimeOffset _spawnTime;

    public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position, double angle = 0.0, Point rotationCenter = new())
        : base(spriteSheet, position, angle, rotationCenter)
    {
        Ttl = ttl;
        _spawnTime = DateTimeOffset.Now;

        // Following band-aid fix should be moved to a BombOject class once it exists
        // Assumes only bombs exist as temporary game objects
        // Not unsubscribing should be fine as SpriteSheet object is local to GameObject
        spriteSheet._externalEventHandler += HandleBombSound;
    }

    // Currently only supports one bomb blowing up at a time
    private void HandleBombSound(int currentFrame, int totalFrames)
    {
        SoundManager soundManager = SoundManager.Instance;
        if (currentFrame < 7)
        {
            soundManager.PlayEffect("BombFuse");
        }
        else if (currentFrame == 7)
        {
            soundManager.StopEffect("BombFuse");
            soundManager.PlayEffect("BombExplode");
        }
        else if (currentFrame >= totalFrames - 1)
        {
            soundManager.StopEffect("BombExplode");
        }
    }

    // Once again, following field assumes this will only class will only be used for bombs
    // Synchronize with the frames in which the bomb actually explodes, not when it expires
    public bool CanKillPlayer { get => SpriteSheet.currentFrame >= 7 && SpriteSheet.currentFrame < 11; }

}