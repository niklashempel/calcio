import sys
import unittest
from pathlib import Path

# Add src to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from fussball_crawler.deobfuscator import Deobfuscator


class TestFussballDeDeobfuscator(unittest.TestCase):
    def setUp(self):
        self.deobfuscator = Deobfuscator()
        data_dir = Path(__file__).parent.parent / "data"

        # Load obfuscated HTML (contains &#xE characters)
        obfuscated_file = data_dir / "fussball_de_original.html"
        if obfuscated_file.exists():
            with open(obfuscated_file, encoding="utf-8") as f:
                self.obfuscated_html = f.read()
        else:
            self.obfuscated_html = None

        # Load expected deobfuscated HTML
        expected_file = data_dir / "fussball_de_expected.html"
        if expected_file.exists():
            with open(expected_file, encoding="utf-8") as f:
                self.expected_html = f.read()
        else:
            self.expected_html = None

    def test_deobfuscate_html(self):
        if self.obfuscated_html is None:
            self.skipTest("Obfuscated HTML file not found")
        if self.expected_html is None:
            self.skipTest("Expected HTML file not found")

        # Act
        result = self.deobfuscator.deobfuscate_html(self.obfuscated_html)

        # Assert - basic checks
        self.assertIsInstance(result, str)
        self.assertGreater(len(result), 0)

        # Assert - obfuscated characters should be removed
        self.assertNotIn("&#xE", result)

        # Assert - result should match expected deobfuscated HTML
        self.assertEqual(result, self.expected_html)


if __name__ == "__main__":
    unittest.main()
