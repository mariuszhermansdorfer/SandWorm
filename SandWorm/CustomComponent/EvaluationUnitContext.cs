namespace SandWorm
{ 
public class EvaluationUnitContext
{
	private EvaluationUnit unit;

	private GH_MenuCollection collection;

	public GH_MenuCollection Collection
	{
		get
		{
			return collection;
		}
		set
		{
			collection = value;
		}
	}

	public EvaluationUnitContext(EvaluationUnit unit)
	{
		this.unit = unit;
		collection = new GH_MenuCollection();
	}
}
}