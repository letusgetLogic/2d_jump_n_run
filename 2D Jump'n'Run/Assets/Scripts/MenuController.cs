using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI creditsText;
 
    
    /// <summary>
    /// On the play button click.
    /// </summary>
    public void OnPlayButtonClick()
    {
        //Scene modusScene = SceneManager.CreateScene("PlayModusScene");
        SceneManager.LoadScene(SceneType.PlayModeScene.ToString());
    }

    /// <summary>
    /// On the credits button click.
    /// </summary>
    public void OnCreditsButtonClick()
    {
        creditsText.enabled = !creditsText.enabled;
    }

    /// <summary>
    /// On the quit button click.
    /// </summary>
    public void OnQuitButtonClick()
    {
        Application.Quit();
    }

    /// <summary>
    /// On the singleplayer button click.
    /// </summary>
    public void OnSingleplayerButtonClick()
    {
        SceneManager.LoadScene(SceneType.LevelScene.ToString());
    }

    /// <summary>
    /// On the back button click.
    /// </summary>
    public void OnBackButtonClick()
    {
        SceneManager.LoadScene(0);
    }
}
