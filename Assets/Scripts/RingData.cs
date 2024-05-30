using System.Collections.Generic;

public class Pipe
{
    public decimal Length { get; set; }
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
    public decimal Rozbiory { get; set; }
    public decimal DesignFlow { get; set; }
    public int FlowDirection { get; set; }
    public decimal Diameter { get; set; }
    public decimal RoundedDiameter { get; set; }
    public decimal Velocity { get; set; }
    public decimal CM { get; set; }
    public decimal K { get; set; }

    public Pipe(decimal velocity)
    {
        Velocity = velocity;
    }
}


public class PipeKey
{
    public string Node1 { get; }
    public string Node2 { get; }

    public PipeKey(string node1, string node2)
    {
        Node1 = node1;
        Node2 = node2;
    }

    public override bool Equals(object obj)
    {
        if (obj is PipeKey otherKey)
        {
            return (Node1 == otherKey.Node1 && Node2 == otherKey.Node2) ||
                   (Node1 == otherKey.Node2 && Node2 == otherKey.Node1);
        }
        return false;
    }

    public override int GetHashCode()
    {
        // Order of nodes doesn't matter for the key, so we ensure the same hash code for the same pair of nodes
        return Node1.GetHashCode() ^ Node2.GetHashCode();
    }
}


public class RingData
{
    public List<IterationData> iterations;
    int ringIndex;
    public int pipesPerRing;


    public Dictionary<PipeKey, Pipe> Pipes { get; set; }
    public List<IterationData> Iterations { get; set; }

    public RingData(int numberOfPipes)
    {
        Pipes = new Dictionary<PipeKey, Pipe>(numberOfPipes);
        IterationData iteration = new IterationData(new List<PipeCalculation>(numberOfPipes));
    }

    public void AddPipe(string node1, string node2, decimal velocity)
    {
        Pipes[new PipeKey(node1, node2)] = new Pipe(velocity);
    }

    public class PipeCalculation
    {
        public decimal Diameter { get; set; }
        public decimal Velocity { get; set; }
        public decimal HeadLoss { get; set; }
        public decimal DesignFlow { get; set; }
        public decimal Quotient { get; set; }
        public decimal DeltaDesignFlow { get; set; }
    }
    public class IterationData
    {
        public List<PipeCalculation> pipeCalculations;

        public IterationData(List<PipeCalculation> pipeCalculations)
        {
            this.pipeCalculations = pipeCalculations;
        }
    }

    public void AddIteration()
    {
        iterations.Add(new IterationData(pipesPerRing));
    }
}
