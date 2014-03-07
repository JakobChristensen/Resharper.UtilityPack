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

  [ContextAction(Name = "Use 'as' operator [Utility Pack]", Description = "Use 'as' operator.", Group = "C#")]
  public class UseAsOperator : ContextActionBase
  {
    #region Constants and Fields

    /// <summary>
    /// The provider.
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// </summary>
    /// <param name="provider">The provider.</param>
    public UseAsOperator(ICSharpContextActionDataProvider provider)
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
        var model = this.GetModel();
        if (model == null)
        {
          return "Use 'as' operator [Utility Pack]";
        }

        return string.Format("Replace with '{0}' [Utility Pack]", model.ReplaceWith);
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

      var expression = this.provider.ElementFactory.CreateExpression(model.ReplaceWith);

      var result = model.Expression.ReplaceBy(expression);
      FormattingUtils.Format(result);

      return null;
    }

    /// <summary>Gets the model.</summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var expression = this.provider.GetSelectedElement<ICastExpression>(true, true);
      if (expression == null)
      {
        return null;
      }

      var operand = expression.Op;
      if (operand == null)
      {
        return null;
      }

      var range = operand.GetNavigationRange();
      if (!range.TextRange.Contains(this.provider.CaretOffset))
      {
        return null;
      }

      if (expression.Op == null)
      {
        return null;
      }

      var typeOperand = expression.TargetType;
      if (typeOperand == null)
      {
        return null;
      }

      var code = string.Format("{0} as {1}", operand.GetText(), typeOperand.GetText());

      return new Model
      {
        Expression = expression,
        ReplaceWith = code
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
      public ICastExpression Expression { get; set; }

      /// <summary>
      /// Gets or sets the replace with.
      /// </summary>
      /// <value>The replace with.</value>
      public string ReplaceWith { get; set; }

      #endregion
    }
  }
}