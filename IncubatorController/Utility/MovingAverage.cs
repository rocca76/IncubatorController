using System.Collections;

namespace NetduinoPlus.Controler
{
    public sealed class MovingAverage
    {
        public int Period = 5;
        private Queue Values = new Queue();

        public void Push(double value)
        {
            if (Values.Count == Period)
            {
                Values.Dequeue();
            }

            Values.Enqueue(value);
        }

        public void Clear()
        {
            Values.Clear();
        }

        public double Average 
        { 
            get 
            {
                if ( Values.Count == 0 )
                {
                    return 0;
                }
                else if (Values.Count < Period )
                {
                    //return (double)Values.Peek();
                    return (double)Values.ToArray().GetValue(Values.Count-1);
                }
                else
                {
                    double average = 0;

                    foreach (double value in Values)
                    {
                        average += value;
                    }

                    return average / (double)Period;
                }
            }
        }
    }
}
