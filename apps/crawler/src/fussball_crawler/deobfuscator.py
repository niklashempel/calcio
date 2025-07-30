import os
import requests
from bs4 import BeautifulSoup
from fontTools.ttLib import TTFont
import logging

class Deobfuscator:
    def __init__(self):
        self.logger = logging.getLogger("Deobfuscator")

    def build_char_mapping(self, font_filename):
        glyph_to_char = {
            # Numbers
            'zero': '0', 'one': '1', 'two': '2', 'three': '3', 'four': '4',
            'five': '5', 'six': '6', 'seven': '7', 'eight': '8', 'nine': '9',
            # Punctuation
            'comma': ',', 'period': '.', 'colon': ':', 'space': ' ',
            'hyphen': '-', 'minus': '-', 'bar': '|', 'pipe': '|',
            # German characters
            'germandbls': 'ß', 'adieresis': 'ä', 'odieresis': 'ö', 'udieresis': 'ü',
            'Adieresis': 'Ä', 'Odieresis': 'Ö', 'Udieresis': 'Ü',
            # Accented characters - lowercase
            'aacute': 'á', 'agrave': 'à', 'acircumflex': 'â', 'atilde': 'ã', 'aring': 'å',
            'eacute': 'é', 'egrave': 'è', 'ecircumflex': 'ê', 'edieresis': 'ë',
            'iacute': 'í', 'igrave': 'ì', 'icircumflex': 'î', 'idieresis': 'ï',
            'oacute': 'ó', 'ograve': 'ò', 'ocircumflex': 'ô', 'otilde': 'õ', 'oslash': 'ø',
            'uacute': 'ú', 'ugrave': 'ù', 'ucircumflex': 'û',
            'yacute': 'ý', 'ydieresis': 'ÿ',
            'ccedilla': 'ç', 'ntilde': 'ñ',
            'ae': 'æ', 'oe': 'œ',
            # Accented characters - uppercase
            'Aacute': 'Á', 'Agrave': 'À', 'Acircumflex': 'Â', 'Atilde': 'Ã', 'Aring': 'Å',
            'Eacute': 'É', 'Egrave': 'È', 'Ecircumflex': 'Ê', 'Edieresis': 'Ë',
            'Iacute': 'Í', 'Igrave': 'Ì', 'Icircumflex': 'Î', 'Idieresis': 'Ï',
            'Oacute': 'Ó', 'Ograve': 'Ò', 'Ocircumflex': 'Ô', 'Otilde': 'Õ', 'Oslash': 'Ø',
            'Uacute': 'Ú', 'Ugrave': 'Ù', 'Ucircumflex': 'Û',
            'Yacute': 'Ý', 'Ydieresis': 'Ÿ',
            'Ccedilla': 'Ç', 'Ntilde': 'Ñ',
            'AE': 'Æ', 'OE': 'Œ',
            # Eastern European characters
            'scaron': 'š', 'Scaron': 'Š', 'zcaron': 'ž', 'Zcaron': 'Ž',
            'cacute': 'ć', 'Cacute': 'Ć', 'ccaron': 'č', 'Ccaron': 'Č',
            'dcaron': 'ď', 'Dcaron': 'Ď', 'ecaron': 'ě', 'Ecaron': 'Ě',
            'lacute': 'ĺ', 'Lacute': 'Ĺ', 'lcaron': 'ľ', 'Lcaron': 'Ľ',
            'nacute': 'ń', 'Nacute': 'Ń', 'ncaron': 'ň', 'Ncaron': 'Ň',
            'racute': 'ŕ', 'Racute': 'Ŕ', 'rcaron': 'ř', 'Rcaron': 'Ř',
            'sacute': 'ś', 'Sacute': 'Ś', 'tcaron': 'ť', 'Tcaron': 'Ť',
            'uring': 'ů', 'Uring': 'Ů', 'zacute': 'ź', 'Zacute': 'Ź',
            'zdotaccent': 'ż', 'Zdotaccent': 'Ż',
            # Common letters (fallback)
            'a': 'a', 'b': 'b', 'c': 'c', 'd': 'd', 'e': 'e', 'f': 'f', 'g': 'g',
            'h': 'h', 'i': 'i', 'j': 'j', 'k': 'k', 'l': 'l', 'm': 'm', 'n': 'n',
            'o': 'o', 'p': 'p', 'q': 'q', 'r': 'r', 's': 's', 't': 't', 'u': 'u',
            'v': 'v', 'w': 'w', 'x': 'x', 'y': 'y', 'z': 'z',
            'A': 'A', 'B': 'B', 'C': 'C', 'D': 'D', 'E': 'E', 'F': 'F', 'G': 'G',
            'H': 'H', 'I': 'I', 'J': 'J', 'K': 'K', 'L': 'L', 'M': 'M', 'N': 'N',
            'O': 'O', 'P': 'P', 'Q': 'Q', 'R': 'R', 'S': 'S', 'T': 'T', 'U': 'U',
            'V': 'V', 'W': 'W', 'X': 'X', 'Y': 'Y', 'Z': 'Z'
        }
        char_mapping = {}
        with TTFont(font_filename) as f:
            cmap = f.getBestCmap()
            if cmap:
                for unicode_codepoint, glyph_name in cmap.items():
                    unicode_char = chr(unicode_codepoint)
                    char_mapping[unicode_char] = glyph_to_char.get(glyph_name, glyph_name)
        return char_mapping

    def deobfuscate_html(self, html: str):
        soup = BeautifulSoup(html, "html.parser")
        all_obfuscated_spans = soup.find_all("span", {"data-obfuscation": True})
        if not all_obfuscated_spans:
            self.logger.debug("No obfuscated spans found")
            return html
        spans_by_id = {}
        from bs4.element import Tag
        for span in all_obfuscated_spans:
            if isinstance(span, Tag):
                span_id = span.get("data-obfuscation")
                if span_id not in spans_by_id:
                    spans_by_id[span_id] = []
                spans_by_id[span_id].append(span)
        for obfuscation_id, spans in spans_by_id.items():
            font_url = f"https://www.fussball.de/export.fontface/-/format/woff/id/{obfuscation_id}/type/font"
            font_file = requests.get(font_url)
            font_filename = f"font_{obfuscation_id}.woff"
            with open(font_filename, "wb") as f:
                f.write(font_file.content)
            char_mapping = self.build_char_mapping(font_filename)
            try:
                for span in spans:
                    if span.string:
                        deobfuscated_text = self._replace_chars(span.string, char_mapping)
                        span.string.replace_with(deobfuscated_text)
                    else:
                        new_contents = []
                        for content in span.contents:
                            if isinstance(content, str):
                                deobfuscated_text = self._replace_chars(content, char_mapping)
                                new_contents.append(deobfuscated_text)
                            else:
                                new_contents.append(content)
                        span.clear()
                        for new_content in new_contents:
                            span.append(new_content)
            finally:
                if os.path.exists(font_filename):
                    os.remove(font_filename)
        return str(soup)

    def _replace_chars(self, text, char_mapping):
        for obfuscated_char, real_char in char_mapping.items():
            text = text.replace(obfuscated_char, real_char)
        # Post-processing cleanup for common patterns
        cleanup_patterns = {
            'germandbls': 'ß', 'udieresis': 'ü', 'adieresis': 'ä', 'odieresis': 'ö',
            'Udieresis': 'Ü', 'Adieresis': 'Ä', 'Odieresis': 'Ö', 'bar': '|',
            ' bar ': ' | ', 'k.A. bar k.A. bar k.A.': 'k.A. | k.A. | k.A.'
        }
        for pattern, replacement in cleanup_patterns.items():
            text = text.replace(pattern, replacement)
        text = text.replace('\xa0', ' ')
        return text
