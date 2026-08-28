using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class WardrobeMenuController : MonoBehaviour
{
    [SerializeField] private Button girlSelectionButton;
    [SerializeField] private Button boySelectionButton;
    [SerializeField] private VideoPlayer girlVideoPlayer;
    [SerializeField] private VideoPlayer boyVideoPlayer;
    [SerializeField] private Image chosenCharacterImage;
    [SerializeField] private Sprite girlProfileSprite;
    [SerializeField] private Sprite boyProfileSprite;

    private void Awake()
    {
        girlSelectionButton.onClick.AddListener(() => SelectCharacter(CharacterChoice.Girl));
        boySelectionButton.onClick.AddListener(() => SelectCharacter(CharacterChoice.Boy));

        SetupHoverPreview(girlSelectionButton.gameObject, girlVideoPlayer);
        SetupHoverPreview(boySelectionButton.gameObject, boyVideoPlayer);
    }

    private void Start()
    {
        ApplySelection(CharacterSelection.Local);
    }

    // Both VideoPlayers are serialized as PlayOnAwake in the scene, which would auto-play them
    // once on load instead of reacting to hover - enforced here so hover is the only trigger.
    // Prepare + Play/Pause (instead of Stop) so the RenderTexture still gets frame 0 drawn into
    // it immediately, rather than staying blank until the player's first hover.
    private void SetupHoverPreview(GameObject buttonObject, VideoPlayer videoPlayer)
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.prepareCompleted += OnPreviewPrepared;
        videoPlayer.Prepare();

        EventTrigger trigger = buttonObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = buttonObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        pointerEnter.callback.AddListener(_ => videoPlayer.Play());
        trigger.triggers.Add(pointerEnter);

        EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        pointerExit.callback.AddListener(_ =>
        {
            videoPlayer.Pause();
            videoPlayer.frame = 0;
        });
        trigger.triggers.Add(pointerExit);
    }

    private void OnPreviewPrepared(VideoPlayer videoPlayer)
    {
        videoPlayer.prepareCompleted -= OnPreviewPrepared;
        videoPlayer.Play();
        videoPlayer.Pause();
    }

    private void SelectCharacter(CharacterChoice choice)
    {
        CharacterSelection.Local = choice;
        ApplySelection(choice);
    }

    private void ApplySelection(CharacterChoice choice)
    {
        chosenCharacterImage.sprite = choice == CharacterChoice.Boy ? boyProfileSprite : girlProfileSprite;
    }
}
