using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DatosJuego
{

    // SALUD

    public int vidaActual = 5;
    public int vidaMaxima = 5;


    // POCIONES

    public int cantidadpociones = 3;
    public int maxPotions = 5; 
    // Nivel espada
    public int nivelActualEspada = 1;
    //Monedas
    public int cantidadMonedas = 0;
    //Nivel actual (Definir si esta en la version final del juego)
    public int level = 1;
    //Posicion del jugador y camara
    public Vector3 posicion = Vector3.zero;
    public Vector3 posicionCamara = Vector3.zero;
    //Escena actual
    public string escenaActual = "";

    public List<string> jefesDerrotados = new List<string>();

}