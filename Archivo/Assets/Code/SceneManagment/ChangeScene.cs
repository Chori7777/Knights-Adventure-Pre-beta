using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class ChangeScene : MonoBehaviour
{
    public static int MainMenuVariation = 0;

    private void CargarEscena(string nombreEscena)
    {
        Time.timeScale = 1f;

        if (FadeController.Instance != null)
        {
            FadeController.Instance.CambiarEscenaConFade(nombreEscena);
        }
        else
        {
            SceneManager.LoadScene(nombreEscena);
        }
    }

    public void LoadScene(string sceneName)
    {
        CargarEscena(sceneName);
    }

    public void pause()
    {
        Time.timeScale = 0f;
    }

    public void resume()
    {
        Time.timeScale = 1f;
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        CargarEscena(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        if (ControladorDatosJuego.Instance != null)
        {
            ControladorDatosJuego.Instance.GuardarDatos();
        }
        Application.Quit();
    }

    public void MainMenu()
    {
        MainMenuVariation = 0;
        CargarEscena("MainMenu");
    }

    public void NewGame()
    {
        BorrarPartidaGuardada();

        if (ControladorDatosJuego.Instance != null)
        {
            ControladorDatosJuego.Instance.ResetearDatos();
            ControladorDatosJuego.Instance.datosjuego.jefesDerrotados.Clear();
        }

        if (MainMenuVariation == 0)
        {
            CargarEscena("CharacterSelector");
        }
        else if (MainMenuVariation == 1)
        {
            CargarEscena("CharacterSelectorAlternative");
        }
    }

    public void ContinueGame()
    {
        if (ExistePartidaGuardada())
        {
            if (ControladorDatosJuego.Instance != null)
            {
                ControladorDatosJuego.Instance.CargarDatos();
                string escenaGuardada = ControladorDatosJuego.Instance.datosjuego.escenaActual;

                if (!string.IsNullOrEmpty(escenaGuardada))
                {
                    CargarEscena(escenaGuardada);
                }
                else
                {
                    CargarEscena("CharacterSelector");
                }
            }
        }
    }

    public bool ExistePartidaGuardada()
    {
        string archivo = Application.persistentDataPath + "/datosjuego.json";
        return File.Exists(archivo);
    }

    private void BorrarPartidaGuardada()
    {
        string archivo = Application.persistentDataPath + "/datosjuego.json";

        if (File.Exists(archivo))
        {
            File.Delete(archivo);
        }
    }

    public void SetMenuVariation(int variation)
    {
        MainMenuVariation = variation;
    }
}
