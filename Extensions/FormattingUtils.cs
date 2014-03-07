// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FormatterUtils.cs" company="">
//   
// </copyright>
// <summary>
//   The formatter utils.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace UtilityPack.Extensions
{
  using JetBrains.Annotations;
  using JetBrains.Application.Progress;
  using JetBrains.DocumentManagers;
  using JetBrains.DocumentModel;
  using JetBrains.ReSharper.Psi;
  using JetBrains.ReSharper.Psi.CodeStyle;
  using JetBrains.ReSharper.Psi.CSharp.CodeStyle;
  using JetBrains.ReSharper.Psi.Tree;

  /// <summary>The formatter utils.</summary>
  public static class FormattingUtils
  {
    #region Public Methods and Operators

    /// <summary>The format body.</summary>
    /// <param name="statement">The body.</param>
    public static void Format([NotNull] ITreeNode statement)
    {
      if (!statement.IsPhysical())
      {
        return;
      }

      var documentRange = statement.GetDocumentRange();
      if (!documentRange.IsValid())
      {
        return;
      }

      var containingFile = statement.GetContainingFile();
      if (containingFile == null)
      {
        return;
      }

      var psiSourceFile = containingFile.GetSourceFile();
      if (psiSourceFile == null)
      {
        return;
      }

      var document = psiSourceFile.Document;
      var solution = statement.GetPsiServices().Solution;

      var languageService = statement.Language.LanguageService();
      if (languageService == null)
      {
        return;
      }

      var codeFormatter = languageService.CodeFormatter;
      if (codeFormatter == null)
      {
        return;
      }

      var rangeMarker = new DocumentRange(document, documentRange.TextRange).CreateRangeMarker(DocumentManager.GetInstance(solution));

      containingFile.OptimizeImportsAndRefs(rangeMarker, false, true, NullProgressIndicator.Instance);
      codeFormatter.Format(statement, CodeFormatProfile.DEFAULT);
    }

    #endregion
  }
}