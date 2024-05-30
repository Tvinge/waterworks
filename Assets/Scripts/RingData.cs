using System.Collections.Generic;

public class RingData
{
    int ringIndex;
    public int pipesPerRing;

    public decimal[] pipeLengths;
    public decimal[] pipeInflows;
    public decimal[] pipeOutflows;
    public decimal[] pipeRozbiory;
    public decimal[] pipeDesignFlow;
    public int[] pipeFlowDirection;
    public decimal[] pipeDiameters;
    public decimal[] roundedPipeDiameters;
    public decimal[] pipeVelocity;
    public decimal[] pipeCM;
    public decimal[] pipeK;

    public List<IterationData> iterations;
    public RingData(int pipesPerRing)
    {
        //this.ringIndex = ringIndex;
        this.pipesPerRing = pipesPerRing;
        pipeLengths = new decimal[pipesPerRing];
        pipeInflows = new decimal[pipesPerRing];
        pipeOutflows = new decimal[pipesPerRing];
        pipeRozbiory = new decimal[pipesPerRing];
        pipeDesignFlow = new decimal[pipesPerRing];
        pipeFlowDirection = new int[pipesPerRing];
        pipeDiameters = new decimal[pipesPerRing];
        roundedPipeDiameters = new decimal[pipesPerRing];
        pipeVelocity = new decimal[pipesPerRing];
        pipeCM = new decimal[pipesPerRing];
        pipeK = new decimal[pipesPerRing];

        iterations = new List<IterationData>();
        
        for (int i = 0; i < pipeVelocity.Length; i++)
        {

            pipeVelocity[i] = 1m;
            //iterations.Add(new IterationData(pipesPerRing));
        }
    }
    public class IterationData
    {
        public decimal[] designFlow;
        public decimal[] headLoses;
        public decimal[] quotient;
        public decimal[] deltaDesignFlow;
        public IterationData(int pipesPerRing)
        {
            designFlow = new decimal[pipesPerRing];
            headLoses = new decimal[pipesPerRing];
            quotient = new decimal[pipesPerRing];
            deltaDesignFlow = new decimal[pipesPerRing];
        }
    }

    public void AddIteration()
    {
        iterations.Add(new IterationData(pipesPerRing));
    }
}
