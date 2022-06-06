namespace Pipoga
{
    class TimeOut
    {
        double threshold;
        double lastTimeout;

        public TimeOut(double threshold)
        {
            this.threshold = threshold;
            this.lastTimeout = 0;
        }

        public bool Lap(double ms)
        {
            if (ms > this.lastTimeout + this.threshold)
            {
                this.lastTimeout = ms;
                return true;
            }
            return false;
        }
    }
}
