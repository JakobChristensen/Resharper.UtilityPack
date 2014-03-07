namespace UtilityPack.Actions
{
  using System;
  using System.Linq;
  using System.Text.RegularExpressions;
  using JetBrains.Annotations;
  using JetBrains.Application.Progress;
  using JetBrains.ProjectModel;
  using JetBrains.ReSharper.Feature.Services.Bulbs;
  using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
  using JetBrains.ReSharper.Intentions.Extensibility;
  using JetBrains.ReSharper.Intentions.Util;
  using JetBrains.ReSharper.Psi.CSharp.Tree;
  using JetBrains.ReSharper.Psi.Tree;
  using JetBrains.TextControl;
  using JetBrains.Util;
  using UtilityPack.Extensions;

  [ContextAction(Name = "Reverse For-Loop [Utility Pack]", Description = "Reverses the direction of a for-loop.", Group = "C#")]
  public class ReverseForLoop : ContextActionBase
  {
    #region Constants and Fields

    /// <summary>
    /// The provider.
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ReverseForLoop"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public ReverseForLoop(ICSharpContextActionDataProvider provider)
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
        return "Reverse for-loop [Utility Pack]";
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the selected for statement.
    /// </summary>
    /// <returns></returns>
    public IForStatement GetSelectedForStatement()
    {
      var forStatement = this.provider.GetSelectedElement<IForStatement>(true, true);
      if (forStatement == null)
      {
        return null;
      }

      var keyword = forStatement.ForKeyword;
      if (keyword == null)
      {
        return null;
      }

      var selectedTreeNode = this.provider.SelectedElement;
      if (selectedTreeNode == null)
      {
        return null;
      }

      if (!keyword.Contains(selectedTreeNode))
      {
        return null;
      }

      return forStatement;
    }

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

      ICSharpExpression exp;

      if (model.PostfixOperatorExpression.PostfixOperatorType == PostfixOperatorType.PLUSPLUS)
      {
        var expression = this.AddToExpression(model.To, '-');

        model.ExpressionInitializer.Value.ReplaceBy(expression);

        var condition = this.GetCondition(model.RelationalExpression.LeftOperand, model.RelationalExpression.OperatorSign.GetText(), model.From);

        model.RelationalExpression.ReplaceBy(condition);

        exp = this.provider.ElementFactory.CreateExpression(string.Format("{0}--", model.PostfixOperatorExpression.Operand.GetText()));
      }
      else
      {
        var expression = this.AddToExpression(model.From, '+');

        model.ExpressionInitializer.Value.ReplaceBy((ICSharpExpression)model.To);

        var condition = this.GetCondition(model.RelationalExpression.LeftOperand, model.RelationalExpression.OperatorSign.GetText(), expression);

        model.RelationalExpression.ReplaceBy(condition);

        exp = this.provider.ElementFactory.CreateExpression(string.Format("{0}++", model.PostfixOperatorExpression.Operand.GetText()));
      }

      model.PostfixOperatorExpression.ReplaceBy(exp);
      FormattingUtils.Format(model.Statement);

      return null;
    }

    /// <summary>
    /// Adds to expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="sign">The sign.</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentNullException"></exception>
    private ICSharpExpression AddToExpression([NotNull] IExpression expression, char sign)
    {
      if (expression == null)
      {
        throw new ArgumentNullException("expression");
      }

      var sign2 = sign == '-' ? '+' : '-';

      var code = expression.GetText();
      if (code.StartsWith("(") && code.EndsWith(")"))
      {
        code = code.Substring(1, code.Length - 2);
      }

      var match = Regex.Match(code, "\\" + sign2 + "\\s*1\\s*$");
      if (match.Success)
      {
        code = code.Substring(0, code.Length - match.Value.Length).Trim();

        if (code.StartsWith("(") && code.EndsWith(")"))
        {
          code = code.Substring(1, code.Length - 2);
        }
      }
      else
      {
        if (expression is IBinaryExpression)
        {
          code = "(" + code + ") " + sign + " 1";
        }
        else
        {
          code += sign + " 1";
        }
      }

      code = code.Trim();

      return this.provider.ElementFactory.CreateExpression(code);
    }

    /// <summary>
    /// Gets the condition.
    /// </summary>
    /// <param name="leftOperand">The left operand.</param>
    /// <param name="operatorSign">The operator sign.</param>
    /// <param name="rightOperand">The right operand.</param>
    /// <returns></returns>
    private ICSharpExpression GetCondition(IExpression leftOperand, string operatorSign, IExpression rightOperand)
    {
      switch (operatorSign)
      {
        case "<":
          operatorSign = ">=";
          break;
        case ">":
          operatorSign = "<=";
          break;
        case "<=":
          operatorSign = ">";
          break;
        case ">=":
          operatorSign = "<";
          break;
      }

      return this.provider.ElementFactory.CreateExpression(string.Format("{0} {1} {2}", leftOperand.GetText(), operatorSign, rightOperand.GetText()));
    }

    /// <summary>Gets the model.</summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var forStatement = this.GetSelectedForStatement();
      if (forStatement == null)
      {
        return null;
      }

      var localVariable = forStatement.InitializerDeclarations.FirstOrDefault();
      if (localVariable == null)
      {
        return null;
      }

      if (localVariable.Type.GetPresentableName(forStatement.Language) != "int")
      {
        return null;
      }

      var expressionInitializer = localVariable.Initial as IExpressionInitializer;
      if (expressionInitializer == null)
      {
        return null;
      }

      var from = expressionInitializer.Value;
      if (from == null)
      {
        return null;
      }

      var relationalExpression = forStatement.Condition as IRelationalExpression;
      if (relationalExpression == null)
      {
        return null;
      }

      var to = relationalExpression.RightOperand;
      if (to == null)
      {
        return null;
      }

      var iterators = forStatement.IteratorExpressions.ToList();
      if (iterators.Count != 1)
      {
        return null;
      }

      var postfixOperatorExpression = iterators.FirstOrDefault() as IPostfixOperatorExpression;
      if (postfixOperatorExpression == null)
      {
        return null;
      }

      return new Model
      {
        Statement = forStatement,
        ExpressionInitializer = expressionInitializer,
        To = to,
        From = from,
        RelationalExpression = relationalExpression,
        PostfixOperatorExpression = postfixOperatorExpression
      };
    }

    #endregion

    /// <summary>Defines the <see cref="Model"/> class.</summary>
    private class Model
    {
      #region Public Properties

      /// <summary>
      /// Gets or sets the expression initializer.
      /// </summary>
      /// <value>The expression initializer.</value>
      [NotNull]
      public IExpressionInitializer ExpressionInitializer { get; set; }

      /// <summary>
      /// Gets or sets from.
      /// </summary>
      /// <value>From.</value>
      [NotNull]
      public IExpression From { get; set; }

      /// <summary>
      /// Gets or sets the postfix operator expression.
      /// </summary>
      /// <value>The postfix operator expression.</value>
      [NotNull]
      public IPostfixOperatorExpression PostfixOperatorExpression { get; set; }

      /// <summary>
      /// Gets or sets the relational expression.
      /// </summary>
      /// <value>The relational expression.</value>
      [NotNull]
      public IRelationalExpression RelationalExpression { get; set; }

      /// <summary>
      /// Gets or sets the statement.
      /// </summary>
      /// <value>The statement.</value>
      public IForStatement Statement { get; set; }

      /// <summary>
      /// Gets or sets to.
      /// </summary>
      /// <value>To.</value>
      [NotNull]
      public IExpression To { get; set; }

      #endregion
    }
  }
}