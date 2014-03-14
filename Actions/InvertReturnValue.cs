namespace UtilityPack.Actions
{
  using System;
  using JetBrains.Annotations;
  using JetBrains.Application.Progress;
  using JetBrains.ProjectModel;
  using JetBrains.ReSharper.Feature.Services.Bulbs;
  using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
  using JetBrains.ReSharper.Intentions.Extensibility;
  using JetBrains.ReSharper.Psi;
  using JetBrains.ReSharper.Psi.CSharp.Tree;
  using JetBrains.ReSharper.Psi.Tree;
  using JetBrains.TextControl;
  using JetBrains.Util;
  using UtilityPack.Extensions;

  /// <summary>
  /// Class DuplicateMethod
  /// </summary>
  [ContextAction(Name = "Invert return value [Utility Pack]", Description = "Inverts the return value of a method that returns a boolean [Utility Pack]", Group = "C#")]
  public class InvertReturnValue : ContextActionBase
  {
    #region Constants and Fields

    /// <summary>
    /// The provider.
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="InvertReturnValue"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public InvertReturnValue([NotNull] ICSharpContextActionDataProvider provider)
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
        return "Invert return value [Utility Pack]";
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

      var processor = new RecursiveElementProcessor(this.ReplaceReturnValue);

      model.Method.ProcessDescendants(processor);

      FormattingUtils.Format(model.Method);

      return null;
    }

    /// <summary>Gets the model.</summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var result = this.provider.GetSelectedElement<IMethodDeclaration>(true, true);
      if (result == null)
      {
        return null;
      }

      var selectedTreeNode = this.provider.SelectedElement;
      if (selectedTreeNode == null)
      {
        return null;
      }

      if (!result.GetNameDocumentRange().Contains(selectedTreeNode.GetDocumentRange()))
      {
        return null;
      }

      var containingType = result.GetContainingTypeDeclaration();
      if (containingType == null)
      {
        return null;
      }

      var declaredElement = result.DeclaredElement;
      if (declaredElement == null)
      {
        return null;
      }

      var type = declaredElement.ReturnType;
      if (type == null)
      {
        return null;
      }

      if (type.GetPresentableName(result.Language) != "bool")
      {
        return null;
      }

      return new Model
      {
        Method = result,
      };
    }

    /// <summary>
    /// Determines whether [is not expression] [the specified value].
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if [is not expression] [the specified value]; otherwise, <c>false</c>.</returns>
    private bool IsNotExpression(ITreeNode value)
    {
      var unaryOperatorExpression = value as IUnaryOperatorExpression;
      if (unaryOperatorExpression == null)
      {
        return false;
      }

      var sign = unaryOperatorExpression.OperatorSign;
      if (sign == null)
      {
        return false;
      }

      var operatorSign = sign.GetText();

      return operatorSign == "!";
    }

    /// <summary>
    /// Replaces the return value.
    /// </summary>
    /// <param name="treeNode">The element.</param>
    private void ReplaceReturnValue(ITreeNode treeNode)
    {
      var returnStatement = treeNode as IReturnStatement;
      if (returnStatement == null)
      {
        return;
      }

      var value = returnStatement.Value;
      if (value == null)
      {
        return;
      }

      var factory = this.provider.ElementFactory;
      if (factory == null)
      {
        return;
      }

      ICSharpExpression expression;

      var text = value.GetText();
      if (text == "true")
      {
        expression = factory.CreateExpression("false");
      }
      else if (text == "false")
      {
        expression = factory.CreateExpression("true");
      }
      else if (this.IsNotExpression(value))
      {
        var unaryOperatorExpression = (IUnaryOperatorExpression)value;

        text = unaryOperatorExpression.Operand.GetText();
        if (text.StartsWith("(") && text.EndsWith(")"))
        {
          text = text.Substring(1, text.Length - 2);
        }

        expression = factory.CreateExpression(text);
      }
      else
      {
        if (text.StartsWith("(") && text.EndsWith(")"))
        {
          text = text.Substring(1, text.Length - 2);
        }

        expression = factory.CreateExpression("!(" + text + ")");
      }

      if (expression != null)
      {
        value.ReplaceBy(expression);
      }
    }

    #endregion

    /// <summary>Defines the <see cref="Model"/> class.</summary>
    private class Model
    {
      #region Public Properties

      /// <summary>
      /// Gets or sets the function.
      /// </summary>
      /// <value>The function.</value>
      [NotNull]
      public IMethodDeclaration Method { get; set; }

      #endregion
    }
  }
}