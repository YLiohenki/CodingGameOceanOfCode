public class Evaluator
{
    public Evaluator(int _height, int _width)
    {
        this.height = _height;
        this.width = _width;
        W = new W();
    }
    private int height;
    private int width;
    private W W;

    public double EvaluateTorpedoFire(double expectedTargetDamage, double firemanDamage, int oldPossibilities, int newPossibilities)
    {
        var result = expectedTargetDamage - firemanDamage;
        result -= W.torpedoDecreaseMyPossibilityFine * (oldPossibilities - newPossibilities) / oldPossibilities;
        return result;
    }

    public double EvaluatePosition()
    {
        return 0;
    }
}

