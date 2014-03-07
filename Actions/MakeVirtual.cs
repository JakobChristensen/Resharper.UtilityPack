namespace UtilityPack.Actions
{
  using System;
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

  /// <summary>
  /// Class MakeVirtual
  /// </summary>
  [ContextAction(Name = "To non-abstract class with virtual members [Utility Pack]", Description = "Converts an abstract class to a normal class with virtual members.", Group = "C#")]
  public class MakeVirtual : ContextActionBase
  {
    #region Constants and Fields

    /// <summary>
    /// 
    /// </summary>
    private readonly ICSharpContextActionDataProvider provider;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="MakeVirtual"/> class.
    /// </summary>
    /// <param name="provider">The provider.</param>
    public MakeVirtual(ICSharpContextActionDataProvider provider)
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
        return "To non-abstract class with virtual members [Utility Pack]";
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

      model.Class.SetAbstract(false);

      foreach (var function in model.Class.MethodDeclarations.Where(f => f.IsAbstract))
      {
        function.SetAbstract(false);
        function.SetVirtual(true);
        if (function.Body == null)
        {
          function.SetBody(this.provider.ElementFactory.CreateEmptyBlock());
        }
      }

      foreach (var property in model.Class.PropertyDeclarations.Where(p => p.IsAbstract))
      {
        property.SetAbstract(false);
        property.SetVirtual(true);

        foreach (var accessor in property.AccessorDeclarations.Where(a => a.Body == null))
        {
          accessor.SetBody(this.provider.ElementFactory.CreateEmptyBlock());
        }
      }

      return null;
    }

    /// <summary>Gets the model.</summary>
    /// <returns>Returns the model.</returns>
    private Model GetModel()
    {
      var selectedElement = this.provider.SelectedElement;
      if (selectedElement == null)
      {
        return null;
      }

      var text = selectedElement.GetText();
      if (text != "abstract")
      {
        return null;
      }

      var @class = this.provider.GetSelectedElement<IClassDeclaration>(true, true);
      if (@class == null)
      {
        return null;
      }

      var typeMember = this.provider.GetSelectedElement<ITypeMemberDeclaration>(true, true);
      if (typeMember != null && typeMember.DeclaredName != @class.DeclaredName)
      {
        return null;
      }

      return new Model
      {
        Class = @class,
      };
    }

    #endregion

    /// <summary>Defines the <see cref="Model"/> class.</summary>
    private class Model
    {
      #region Public Properties

      /// <summary>
      /// Gets or sets the class.
      /// </summary>
      /// <value>The class.</value>
      [NotNull]
      public IClassDeclaration Class
      {
        get;
        set;
      }

      #endregion
    }
  }
}