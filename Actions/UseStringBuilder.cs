namespace UtilityPack.Actions
{
  using System;
  using System.Collections.Generic;
  using JetBrains.Annotations;
  using JetBrains.Application.Progress;
  using JetBrains.ProjectModel;
  using JetBrains.ReSharper.Feature.Services.Bulbs;
  using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
  using JetBrains.ReSharper.Intentions.Extensibility;
  using JetBrains.ReSharper.Psi.CSharp.Tree;
  using JetBrains.ReSharper.Psi.Tree;
  using JetBrains.TextControl;
  using JetBrains.Util;
  using UtilityPack.Extensions;

  /// <summary>
  /// Defines the PullParameters class.
  /// </summary>
  [ContextAction(Name = "Use StringBuilder [UtilityPack]", Description = "Converts concatenation of a few strings and other objects to the use of StringBuilder class.", Group = "C#")]
  public class UseStringBuilder : ContextActionBase
  {
    #region Constants and Fields

    /// <summary>
    /// The provider
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UseStringBuilder"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public UseStringBuilder(ICSharpContextActionDataProvider provider)
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
        return "Use StringBuilder [Utility Pack]";
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

      var statement = this.provider.ElementFactory.CreateStatement("var stringBuilder = new System.Text.StringBuilder();");

      model.Block.AddStatementBefore(statement, (ICSharpStatement)model.Anchor);

      foreach (var expression in model.Expressions)
      {
        statement = this.provider.ElementFactory.CreateStatement("stringBuilder.Append(" + expression.GetText() + ");");

        model.Block.AddStatementBefore(statement, (ICSharpStatement)model.Anchor);
      }

      var toString = this.provider.ElementFactory.CreateExpression("stringBuilder.ToString()");

      model.AdditiveExpression.ReplaceBy(toString);

      FormattingUtils.Format(model.Block);

      return null;
    }

    /// <summary>Builds the addition list.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="expressions">The expressions.</param>
    /// <returns>Returns <c>true</c>, if successful, otherwise <c>false</c>.</returns>
    private bool GetExpressions([NotNull] IExpression expression, [NotNull] List<IExpression> expressions)
    {
      if (expression.Type().GetLongPresentableName(expression.Language) == "string")
      {
        var literalExpression = expression as ILiteralExpression;
        if (literalExpression != null)
        {
          expressions.Add(expression);
          return true;
        }

        var additiveExpression = expression as IAdditiveExpression;
        if (additiveExpression != null)
        {
          var left = this.GetExpressions(additiveExpression.LeftOperand, expressions);
          var right = this.GetExpressions(additiveExpression.RightOperand, expressions);

          return left || right;
        }
      }

      expressions.Add(expression);

      return false;
    }

    /// <summary>
    /// Gets the model.
    /// </summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var anchor = this.provider.GetSelectedElement<IStatement>(true, true);
      if (anchor == null)
      {
        return null;
      }

      var block = anchor.GetContainingNode<IBlock>();
      if (block == null)
      {
        return null;
      }

      var additiveExpression = this.provider.GetSelectedElement<IAdditiveExpression>(true, true);
      if (additiveExpression == null)
      {
        return null;
      }

      if (additiveExpression.Type().GetLongPresentableName(additiveExpression.Language) != "string")
      {
        return null;
      }

      var expressions = new List<IExpression>();

      if (!this.GetExpressions(additiveExpression, expressions))
      {
        return null;
      }

      foreach (var expression in expressions)
      {
        var literalExpression = expression as ILiteralExpression;

        if (literalExpression == null || literalExpression.Type().GetLongPresentableName(literalExpression.Language) != "string")
        {
          return null;
        }
      }

      return new Model
      {
        AdditiveExpression = additiveExpression,
        Expressions = expressions,
        Block = block,
        Anchor = anchor
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
      public IAdditiveExpression AdditiveExpression { get; set; }

      /// <summary>
      /// Gets or sets the anchor.
      /// </summary>
      /// <value>The anchor.</value>
      public IStatement Anchor { get; set; }

      /// <summary>
      /// Gets or sets the block.
      /// </summary>
      /// <value>The block.</value>
      public IBlock Block { get; set; }

      /// <summary>
      /// Gets or sets the expressions.
      /// </summary>
      /// <value>The expressions.</value>
      public List<IExpression> Expressions { get; set; }

      #endregion
    }
  }
}