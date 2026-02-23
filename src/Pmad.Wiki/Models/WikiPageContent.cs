namespace Pmad.Wiki.Models;

public record WikiPageContent(WikiPageFrontMatter FrontMatter, string ContentWithoutFrontMatter);
