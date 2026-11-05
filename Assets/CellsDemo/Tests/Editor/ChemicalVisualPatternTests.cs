#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Cells.Rendering;
using Cells.Substances;

namespace Cells.Tests
{
    public sealed class ChemicalVisualPatternTests
    {
        [Test]
        public void PatternIsDeterministicAndFormDependent()
        {
            float a=ChemicalVisualPattern.Mask(MaterialForm.Suspension,7,13,631);
            float b=ChemicalVisualPattern.Mask(MaterialForm.Suspension,7,13,631);
            Assert.AreEqual(a,b);
            Assert.AreEqual(1f,ChemicalVisualPattern.Mask(MaterialForm.Dissolved,7,13,631));
            int differences=0;
            for(int y=0;y<12;y++)
            for(int x=0;x<12;x++)
                if(ChemicalVisualPattern.Mask(MaterialForm.Suspension,x,y,631)
                    != ChemicalVisualPattern.Mask(MaterialForm.Dissolved,x,y,631)) differences++;
            Assert.Greater(differences,0);
        }
    }
}
#endif
