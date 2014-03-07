namespace UtilityPack.Actions
{
  using System;
  using System.Diagnostics;
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

  /// <summary>
  /// Class DuplicateMethod
  /// </summary>
  [ContextAction(Name = "Duplicate method [Utility Pack]", Description = "Duplicates a method [Utility Pack]", Group = "C#")]
  public class DuplicateMethod : ContextActionBase
  {
    #region Constants and Fields

    /// <summary>
    /// The provider.
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateMethod"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public DuplicateMethod(ICSharpContextActionDataProvider provider)
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
        return "Duplicate method [Utility Pack]";
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

      var code = model.Method.GetText();
      if (string.IsNullOrEmpty(code))
      {
        return null;
      }

      var typeMember = this.provider.ElementFactory.CreateTypeMemberDeclaration(code);

      var classDeclaration = model.ContainingType as IClassLikeDeclaration;
      if (classDeclaration == null)
      {
        return null;
      }

      var memberDeclaration = typeMember as IClassMemberDeclaration;
      Debug.Assert(memberDeclaration != null, "memberDeclaration != null");

      var result = classDeclaration.AddClassMemberDeclarationBefore(memberDeclaration, model.Method);

      FormattingUtils.Format(result);

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

      return new Model
      {
        Method = result,
        ContainingType = containingType
      };
    }

    #endregion

    /// <summary>Defines the <see cref="Model"/> class.</summary>
    private class Model
    {
      #region Public Properties

      /// <summary>
      /// Gets or sets the type of the containing.
      /// </summary>
      /// <value>The type of the containing.</value>
      [NotNull]
      public ICSharpTypeDeclaration ContainingType { get; set; }

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