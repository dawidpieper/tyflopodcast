# Tyflopodcast
Tyflopodcast to działający od przeszło dziesięciu lat pierwszy polski podcast dla niewidomych.
Prezentowane materiały dotyczą przede wszystkim tyflotechnologii, ale także wszystkich innych aspektów związanych z brakiem wzroku.
Więcej szczegółów można przeczytać na [stronie projektu](http://tyflopodcast.net).
Ten program powstał w celu uproszczenia korzystania z zasobów Tyflopodcastu, zwłaszcza przez początkujących użytkowników komputera.
# Budowanie
Wymagane jest użycie narzędzia csc (technologia .net framework). Przykładowo można użyć konsoli programu Visual Studio.
Przygotowane są skrypty __compile.bat do kompilacji projektu i jego zależności oraz __package.bat tworzący pliki wynikowe, korzystając z pakietu [ILMerge](https://github.com/dotnet/ILMerge]) który należy zainstalować przy użyciu menedżera pakietów NuGet.
Dla ułatwienia w repozytorium umieściłem skompilowane pliki używanych bibliotek [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json), [Bass.Net](http://bass.radio42.com/) oraz [HtmlAgilityPack](https://html-agility-pack.net/).
# Kontrybucje
To nie jest złożony projekt rozpisany na wieloletni rozwój, a raczej mała aplikacja stworzona po godzinach.
Jeśli jednak ktoś jest zainteresowany współpracą lub dodaniem swojej cegiełki, oczywiście do tego zapraszam poprzez umieszczenie Pull Requestu.
# Licencja
General Public License V3