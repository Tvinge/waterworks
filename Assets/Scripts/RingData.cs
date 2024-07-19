using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;
using UnityEngine.Experimental.AI;

public class Node
{
    public int index;
    public decimal inflow;
    public decimal outflow;
    public decimal rozbiory;
    public decimal location;
    public List<Pipe> ConnectedPipes { get; set; } = new List<Pipe>();

}
public class Pipe
{
    public int index { get; set; }
    public decimal length {get; set;}
    public decimal inflow { get; set; }
    public decimal outflow { get; set; }
    public decimal rozbiory { get; set; }
    public decimal designFlow { get; set; }
    public bool flowDirection { get; set; }
    public decimal diameter { get; set; }
    public decimal roundedDiameter { get; set; }
    public decimal velocity { get; set; }
    public decimal cmValue { get; set; }
    public decimal kValue { get; set; }
    public Node lowerNode { get; set; }
    public Node upperNode { get; set; }

    public Pipe(int index)
    {
        this.index = index;
        //this.velocity = velocity;
    }
}


public class PipeKey
{
    public int node1 { get; }
    public int node2 { get; }

    public PipeKey(int _node1, int _node2)
    {
        node1 = _node1;
        node2 = _node2;
    }

    public override bool Equals(object obj)
    {
        if (obj is PipeKey otherKey)
        {
            return (node1 == otherKey.node1 && node2 == otherKey.node2) || 
                   (node1 == otherKey.node2 && node2 == otherKey.node1);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return node1.GetHashCode() ^ node2.GetHashCode();
    }
}

//ITERATIONS JEST LISTA ITERATIONDATA KTORA Z KOLEJI JEST LISTA OBLICZONYCH RUR/ POJEDYNCZEGO PIERSCIENIA
public class RingData
{
    int ringIndex;
    List<int> nodesInRing;

    public int pipesPerRing;

    public Dictionary<PipeKey, Pipe> PipesDictionary { get; set; }
    public List<Pipe> Pipes;
    public List<Node> Nodes;
    public Dictionary<PipeKey, bool> PipesInMultipleRings { get; set; }
    public List<Pipe> PipesInManyRings { get; set; }

    //liczba iteracji
    public List<IterationData> Iterations { get; set; }


    public RingData (int numberOfPipes)
    {
        PipesDictionary = new Dictionary<PipeKey, Pipe>(numberOfPipes);
        PipesInMultipleRings = new Dictionary<PipeKey, bool>(numberOfPipes);

        IterationData iterations = new IterationData(new List<PipeCalculation>(numberOfPipes));
        pipesPerRing = numberOfPipes;
    }
    
    /*
    public void AddPipe(int node1, int node2, decimal velocity)
    {
        Pipes[new PipeKey(node1, node2)] = new Pipe();
    }*/

    public class PipeCalculation
    {
        //public decimal Diameter { get; set; }
        //public decimal Velocity { get; set; }
        public int Index { get; set; }
        public decimal DesignFlow { get; set; }
        public decimal HeadLoss { get; set; }
        public decimal Quotient { get; set; }
        public decimal DeltaDesignFlow { get; set; }
        public decimal KValue { get; set; }
        public decimal finalVelocity { get; set; }
    }

    //pojedyncza iteracja zawiera liste obliczen dla kazdej rury w pierscieniu
    public class IterationData
    {
        //lista obliczen dla kazdej rury w pierscieniu
        public List<PipeCalculation> pipeCalculations;
        public decimal deltaDesignFlowForWholeRing;

        public IterationData(List<PipeCalculation> pipeCalculations)
        {
            this.pipeCalculations = pipeCalculations;
        }
    }
}

