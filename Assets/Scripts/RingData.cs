using System.Collections.Generic;
using UnityEngine;

public class Pipe
{
    public decimal index { get; set; }
    public decimal length {get; set;}
    public decimal inflow { get; set; }
    public decimal outflow { get; set; }
    public decimal rozbiory { get; set; }
    public decimal designFlow { get; set; }
    public int flowDirection { get; set; }
    public decimal diameter { get; set; }
    public decimal roundedDiameter { get; set; }
    public decimal velocity { get; set; }
    public decimal cmValue { get; set; }
    public decimal kValue { get; set; }


    public Pipe(decimal velocity)
    {
        this.velocity = velocity;
    }
}
public class PipeKey
{
    public string node1 { get; }
    public string node2 { get; }

    public PipeKey(string _node1, string _node2)
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
    public int pipesPerRing;
    public decimal deltaDesignFlowForWholeRing;
    public Dictionary<PipeKey, Pipe> Pipes { get; set; }

    //liczba iteracji
    public List<IterationData> Iterations { get; set; }

    public RingData (int numberOfPipes)
    {
        Pipes = new Dictionary<PipeKey, Pipe>(numberOfPipes);
        IterationData iterations = new IterationData(new List<PipeCalculation>(numberOfPipes));
        pipesPerRing = numberOfPipes;
    }
    
    public void AddPipe(string node1, string node2, decimal velocity)
    {
        Pipes[new PipeKey(node1, node2)] = new Pipe(velocity);
    }

    public class PipeCalculation
    {
        //public decimal Diameter { get; set; }
        //public decimal Velocity { get; set; }
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
        public IterationData(List<PipeCalculation> pipeCalculations)
        {
            this.pipeCalculations = pipeCalculations;
        }
    }
}
