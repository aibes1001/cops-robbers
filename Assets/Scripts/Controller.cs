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
                //Agafa la casella dins de cada fila
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                //Posa a cada casella en la seua posició dins de l'array tiles (de 0 a 63)
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //TODO: Inicializar matriz a 0's
        for (int i = 0; i < Constants.TilesPerRow; i++)
        {
            for (int j = 0; j < Constants.TilesPerRow; j++)
            {
                matriu[i, j] = 0;
            }
        }


        //TODO: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        //TODO: Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                int adjacentFila1 = i + 1;
                int adjacentFila0 = i - 1;
                int adjacentColumna1 = i + 8;
                int adjacentColumna0 = i - 8;

                Debug.Log(adjacentColumna1);

                if(i == j)
                {
                    if (adjacentFila1 >= 0 && adjacentFila1 % Constants.TilesPerRow != 0)
                    {
                        tiles[i].adjacency.Add(adjacentFila1);
                        matriu[i, i + 1] = 1;
                    }
                    if (adjacentFila0 >= 0 && adjacentFila0 % Constants.TilesPerRow != 7)
                    {
                        tiles[i].adjacency.Add(adjacentFila0);
                        matriu[i, i - 1] = 1;
                    }
                    if (adjacentColumna1 < Constants.NumTiles)
                    {
                        tiles[i].adjacency.Add(adjacentColumna1);
                        matriu[i, i + 8] = 1;
                    }
                    if (adjacentColumna0 >= 0)
                    {
                        tiles[i].adjacency.Add(adjacentColumna0);
                        matriu[i, i - 8] = 1;
                    }

                }
                

            }
        }

        //Imprimir la matriu d'adjacència
        string fila = "";
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            string columna = "";

            for (int j = 0; j < Constants.NumTiles; j++)
            {
                columna += matriu[i, j].ToString();

            }
            fila += columna + "\n";
        }
        Debug.Log(fila);
    }


    void casillasAdyacentesMatriz(int[,] matriu, GameObject ficha, bool isCop)
    {
        int posicionFicha;

        if (isCop)
        {
            posicionFicha = ficha.GetComponent<CopMove>().currentTile;
        }
        else
        {
            posicionFicha = ficha.GetComponent<RobberMove>().currentTile;
        }

        int fila = posicionFicha / Constants.TilesPerRow;
        int columna = posicionFicha % Constants.TilesPerRow;
        if (fila + 1 < Constants.TilesPerRow)
        {
            matriu[fila + 1, columna] = 1;

        }
        if (fila - 1 >= 0)
        {
            matriu[fila - 1, columna] = 1;
        }

        if (columna + 1 < Constants.TilesPerRow)
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
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
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

        List<Tile> selectableTiles = new List<Tile>();
        int[] distanciaMasLejos = new int[2];
        int casillaMasLejos = -1;


        foreach (Tile t in tiles)
        {
            //Per a cada casella, si està seleccionada, comprovem la distància als policies
            if (t.selectable)
            {
                int[] distancia = distanciaCasillaCops(t);

                //Si la distància més pròxima a un policia de la casella és major que la que estava guardada
                if (distancia[0] > distanciaMasLejos[0])
                {
                    distanciaMasLejos = distancia;
                    casillaMasLejos = t.numTile;
                }

                //En cas de tindre la mateixa distància mínima de 2 caselles, escollir la casella més llunyana de l'altre policia
                else if(distancia[0] == distanciaMasLejos[0])
                {
                    if(distancia[1] >= distanciaMasLejos[1])
                    {
                        distanciaMasLejos = distancia;
                        casillaMasLejos = t.numTile;
                    }
                    
                }
            }
        }
        Debug.Log("Casilla mas lejos: " + casillaMasLejos);

        //Actualitzem la casella actual del caco a la més llunyana als 2 policies i movem el caco a eixa casella
        robber.GetComponent<RobberMove>().currentTile = tiles[casillaMasLejos].numTile;
        robber.GetComponent<RobberMove>().MoveToTile(tiles[robber.GetComponent<RobberMove>().currentTile]);
        Debug.Log("Casilla seleccionada: " + robber.GetComponent<RobberMove>().currentTile);
    }

    //Calcula la distancia a cada policia passant com a parametre una casella i retorna una llista d'enters amb les distàncies als 2 polis
    int[] distanciaCasillaCops(Tile t)
    {
        int casilla = t.numTile;
        int[] distancias = new int[2];
        int cont = 0;
        foreach (GameObject cop in cops)
        {
            int casillaCop = cop.GetComponent<CopMove>().currentTile;
            //Obtenim la fila i columna de la casella que ocupa el policia i l'altra que se li passa
            int filaCop = casillaCop / Constants.TilesPerRow;
            int columnaCop = casillaCop % Constants.TilesPerRow;
            int filaT = casilla / Constants.TilesPerRow;
            int columnaT = casilla % Constants.TilesPerRow;

            //Mesurem la diferència entre les caselles
            int sumaCasillas = System.Math.Abs(filaCop - filaT) + System.Math.Abs(columnaCop - columnaT);
            distancias[cont] = sumaCasillas;
            cont++;
        }

        //Ordenar la llista per a tindre primer la distància al policia més pròxim
        if (distancias[0] > distancias[1])
        {
            int aux = distancias[0];
            distancias[0] = distancias[1];
            distancias[1] = aux;
            return distancias;
        }
        return distancias;
    }

    public void EndGame(bool end)
    {
        if (end)
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

        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        //TODO: Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        //Tendrás que cambiar este código por el BFS

        tiles[indexcurrentTile].visited = true;
        nodes.Enqueue(tiles[indexcurrentTile]);

        while (nodes.Count > 0)
        {
            Tile t = nodes.Dequeue();
            if (t.distance < 2)
            {
                foreach (int num in t.adjacency)
                {
                    //Evita entrar a un poli a la casella d'un altre, i al caco suicidar-se 
                    bool ocupada = deselectableTileCops(tiles[num]);
                    if (!tiles[num].visited && !ocupada)
                    {
                        tiles[num].parent = t;
                        tiles[num].selectable = true;
                        tiles[num].distance = t.distance + 1;
                        tiles[num].visited = true;
                        nodes.Enqueue(tiles[num]);
                    }
                }
            }
        }
    }

    //Mètode per a comprovar si la casella que se li passa correspon a la d'un poli
    bool deselectableTileCops(Tile t)
    {
        foreach (GameObject cop in cops)
        {
            int num = cop.GetComponent<CopMove>().currentTile;
            if (num == t.numTile)
            {
                return true;
            }
        }
        return false;
    }
}
