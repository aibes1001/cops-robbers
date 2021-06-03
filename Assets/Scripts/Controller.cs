using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            //board té les files, i dins de cada fila té les caselles (ací agafa cada fila)
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                //Agafa la casella dis de cada fila
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject; 
                //Posa a cada casella en la seua posició dins de l'array tiles (de 0 a 63)
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //TODO: Inicializar matriz a 0's
        for(int i = 0; i < Constants.NumTiles; i++)
        {
            for(int j = 0; j < Constants.NumTiles; j++)
            {
                matriu[i, j] = 0;
            }
        }

        
        //TODO: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        foreach (GameObject cop in cops)
        {
            //Passem un gameobject i si es cop(true) o robber(false);
            casillasAdyacentesMatriz(matriu, cop, true);
        }

        casillasAdyacentesMatriz(matriu, robber, false);

        Debug.Log(matriu[1, 0]);
        Debug.Log(matriu[7, 4]);
        //TODO: Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        foreach (Tile tile in tiles)
        {
            //Casilla adjacent de "dalt"
            if (tile.numTile - Constants.TilesPerRow >= 0)
            {
                tile.adjacency.Add(tile.numTile - Constants.TilesPerRow);
            }
            //Casilla adjacent de "baix"
            if (tile.numTile + Constants.TilesPerRow <= 63)
            {
                tile.adjacency.Add(tile.numTile + Constants.TilesPerRow);
            }
            //Casilla adjacent de "dreta"
            if (tile.numTile % Constants.TilesPerRow != 7) // si no és una del costat dret
            {
                tile.adjacency.Add(tile.numTile + 1);
            }
            //Casilla adjacent de "esquerra"
            if (tile.numTile % Constants.TilesPerRow != 0) // si no és una del costat esquerra
            {
                tile.adjacency.Add(tile.numTile - 1);
            }
        }
    }

    void casillasAdyacentesMatriz(int[,] matriu, GameObject ficha, bool isCop)
    {
        int posicionFicha;
        int fila;
        int columna;
        if (isCop)
        {
            posicionFicha = ficha.GetComponent<CopMove>().currentTile;
        }
        else
        {
            posicionFicha = ficha.GetComponent<RobberMove>().currentTile;
        }

        fila = posicionFicha / Constants.TilesPerRow;
        columna = posicionFicha % Constants.TilesPerRow;
        if (fila + 1 <= 7)
        {
            matriu[fila + 1, columna] = 1;
        }
        if (fila - 1 >= 0)
        {
            matriu[fila - 1, columna] = 1;
        }

        if (columna + 1 <= 7)
        {
            matriu[fila, columna + 1] = 1;
        }
        if (columna - 1 >= 0)
        {
            matriu[fila, columna - 1] = 1;
        }
    }

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        /*TODO: Cambia el código de abajo para hacer lo siguiente
        - Elegimos una casilla aleatoria entre las seleccionables que puede ir el caco
        - Movemos al caco a esa casilla
        - Actualizamos la variable currentTile del caco a la nueva casilla
        */
        robber.GetComponent<RobberMove>().MoveToTile(tiles[robber.GetComponent<RobberMove>().currentTile]);
    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
                 
        int indexcurrentTile;        

        if (cop==true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        //TODO: Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        //Tendrás que cambiar este código por el BFS
        for(int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].selectable = true;
        }
    }   
}
