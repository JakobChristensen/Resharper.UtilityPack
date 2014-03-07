namespace UtilityPack.Actions
{
  using System;
  using JetBrains.Annotations;
  using JetBrains.Application.Progress;
  using JetBrains.ProjectModel;
  using JetBrains.ReSharper.Feature.Services.Bulbs;
  using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
  using JetBrains.ReSharper.Intentions.Extensibility;
  using JetBrains.ReSharper.Intentions.Util;
  using JetBrains.ReSharper.Psi.CSharp.Tree;
  using JetBrains.TextControl;
  using JetBrains.Util;
  using UtilityPack.Extensions;

  /// <summary>
  /// Defines the UseCompare class.
  /// </summary>
  [ContextAction(Name = "Convert '==' to 'Compare' [Utility Pack]", Description = "Converts usage of equality operator ('==') to a call to string.Compare method.", Group = "C#")]
  public class UseCompare : ContextActionBase
  {
    #region Constants and Fields

    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UseCompare"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public UseCompare(ICSharpContextActionDataProvider provider)
    {
      this.provider = provider;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Popup menu item text
    /// </summary>
    /// <value>The text.</value>
    public override string Text
    {
      get
      {
        return "Use Compare [Utility Pack]";
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines whether the specified cache is available.
    /// </summary>
    /// <param name="cache">The cache.</param>
    /// <returns><c>true</c> if the specified cache is available; otherwise, <c>false</c>.</returns>
    public override bool IsAvailable(IUserDataHolder cache)
    {
      return this.GetModel() != null;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Executes QuickFix or ContextAction. Returns post-execute method.
    /// </summary>
    /// <param name="solution">The solution.</param>
    /// <param name="progress">The progress.</param>
    /// <returns>Action to execute after document and PSI transaction finish. Use to open TextControls, navigate caret, etc.</returns>
    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
      var model = this.GetModel();
      if (model == null)
      {
        return null;
      }

      var code = string.Format("string.Compare({0}, {1}, System.StringComparison.InvariantCultureIgnoreCase) {2} 0", model.Expression.LeftOperand.GetText(), model.Expression.RightOperand.GetText(), model.Expression.OperatorSign.GetText());

      var compareExpression = this.provider.ElementFactory.CreateExpression(code);

      var expression = model.Expression.ReplaceBy(compareExpression);
      FormattingUtils.Format(expression);

      return null;
    }

    /// <summary>
    /// Gets the model.
    /// </summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var expression = this.provider.GetSelectedElement<IEqualityExpression>(true, true);
      if (expression == null)
      {
        return null;
      }

      var operatorSign = expression.OperatorSign;
      if (operatorSign == null)
      {
        return null;
      }

      if (string.IsNullOrEmpty(operatorSign.GetText()))
      {
        return null;
      }

      var rightOperand = expression.RightOperand;
      if (rightOperand == null || rightOperand.Type().GetPresentableName(rightOperand.Language) != "string")
      {
        return null;
      }

      var leftOperand = expression.LeftOperand;
      if (leftOperand == null || leftOperand.Type().GetPresentableName(rightOperand.Language) != "string")
      {
        return null;
      }

      var range = operatorSign.GetNavigationRange();
      if (!range.TextRange.Contains(this.provider.CaretOffset))
      {
        return null;
      }

      return new Model
      {
        Expression = expression
      };
    }

    #endregion

    /// <summary>Defines the <see cref="Model"/> class.</summary>
    private class Model
    {
      #region Public Properties

      /// <summary>
      /// Gets or sets the expression.
      /// </summary>
      /// <value>The expression.</value>
      [NotNull]
      public IBinaryExpression Expression { get; set; }

      #endregion
    }
  }
}