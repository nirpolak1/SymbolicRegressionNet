# Benchmark V0.15 (Academic & Standard Execution)

**Date:** 2026-03-01 16:31:11
**Engine:** SymbolicRegressionNet V0.15
**Total Problems:** 39
**Total Runtime:** 3.8 minutes (230s)
**Settings:** Pop=500, Gen=200, Depth=8, TimeLimit=30s/problem, Seed=42

## Overall Scorecard (SRBench Criteria)

| Metric | Count | Rate |
|--------|-------|------|
| R² ≥ 0.999 (Exact Recovery) | 20 / 39 | 51.3% |
| R² ≥ 0.99 (Excellent) | 24 / 39 | 61.5% |
| R² ≥ 0.95 (Good) | 30 / 39 | 76.9% |
| R² ≥ 0.80 (Fair) | 34 / 39 | 87.2% |
| Mean R² (all problems) | — | 0.8929 |
| Median R² (all problems) | — | 0.9990 |
| Average Time per Problem | — | 5.86s |

## Detailed Per-Suite Results
### Feynman (10 problems)
**Exact (R²≥0.999):** 5/10 (50%) | **Good (R²≥0.95):** 8/10 (80%) | **Mean R²:** 0.8828
| Problem | Target Formula | R² | MSE | Grade | Discovered Expression | Time |
|---------|---------------|-----|-----|-------|-----------------------|------|
| Feynman_Feynman_Coulomb | `k*q1*q2/r^2` | 1.000000 | 8.25E-022 | ★★★ Exact | `(1.01506*(((x2-(x2+3.72289e-12))+x0)*(-1.07857e-12-((x1/x...` | 5.1s |
| Feynman_Feynman_Gravity | `G*m1*m2/r^2` | 0.999597 | 1.36E+001 | ★★★ Exact | `((xx1/x2)*(((((xx1/x2)/-0.126242)/(sin((-3.79632-(x2+x1))...` | 2.6s |
| Feynman_Feynman_KineticE | `0.5*m*v^2` | 1.000000 | 2.10E-028 | ★★★ Exact | `((0.5*x0)*x1)` | 12.7s |
| Feynman_Feynman_IdealGas | `n*R*T/V` | 0.992942 | 9.81E+004 | ★★ Excellent | `((x1*(-1.56646-(log((x2-0.960091))/0.591208)))+(((923.323...` | 3.1s |
| Feynman_Feynman_Pendulum | `2*pi*sqrt(L/g)` | 0.968348 | 9.53E-002 | ★ Good | `(x((((x0-3.70656)-(1.44815*(((x0-3.10118)-logx0)/-0.86359...` | 3.0s |
| Feynman_Feynman_Ohm | `I*R` | 1.000000 | 0.00E+000 | ★★★ Exact | `x0` | 18.9s |
| Feynman_Feynman_Spring | `0.5*k*x^2` | -1.000000 | 1.00E+100 | ✗ Fail | `(((exp(((x0/x1)/(0.285234/x0)))*((expx0.63056).829717))*(...` | 2.9s |
| Feynman_Feynman_RelKinetic | `0.5*m*v^2 + (3/8)*m*v^4/c^2` | 0.995534 | 2.87E+000 | ★★ Excellent | `(x1+(((0.29526*(x1/(0.107304)))*(x0*(logx1-(xexpx1/-63.51...` | 2.8s |
| Feynman_Feynman_Snell | `n1*sin(theta1)/n2` | 0.999840 | 2.67E-005 | ★★★ Exact | `(((0.993912*(0.993912*sinx2))/x1)/exp(sin((-0.740021*(x0....` | 4.1s |
| Feynman_Feynman_GravPotential | `-G*m1*m2/r` | 0.872146 | 1.26E+003 | ~ Fair | `(((979431/((-3.72337+x0)*(((130862/x2)+-68882.9)-((68782....` | 2.4s |

### Keijzer (6 problems)
**Exact (R²≥0.999):** 4/6 (67%) | **Good (R²≥0.95):** 5/6 (83%) | **Mean R²:** 0.9505
| Problem | Target Formula | R² | MSE | Grade | Discovered Expression | Time |
|---------|---------------|-----|-----|-------|-----------------------|------|
| Keijzer_Keijzer4 | `0.3*x*sin(2*pi*x)` | 1.000000 | 7.48E-019 | ★★★ Exact | `((-0.3*cos((-88.0697-(((0.036188x0)-(-4.247*x0))-((-1.465...` | 9.8s |
| Keijzer_Keijzer5 | `x^3*exp(-x)*cos(x)*sin(x)*(sin(x)^2*cos(x)-1)` | 0.719492 | 2.53E-002 | ✗ Fail | `((((0.0342303*(1.56574*sin(((1.90396*x0)+3.5075))))*(x-8....` | 2.8s |
| Keijzer_Keijzer6 | `(30*x*z)/((x-10)*y^2)` | 0.983341 | 5.84E+002 | ★ Good | `((x2+((x0/(3.58778*(x((-0.531552*x1)-logx1))))-((((0.5079...` | 2.6s |
| Keijzer_Keijzer8 | `ln(x)` | 1.000000 | 0.00E+000 | ★★★ Exact | `logx0` | 17.5s |
| Keijzer_Keijzer9 | `sqrt(x)` | 0.999990 | 5.19E-005 | ★★★ Exact | `((3.77005+logx0)-((((16.2988*x0)-707.384)/((144.778+(cos(...` | 4.0s |
| Keijzer_Keijzer14 | `6*sin(x)*cos(y)` | 1.000000 | 0.00E+000 | ★★★ Exact | `(cosx1*(6*sinx0))` | 14.2s |

### Korns (5 problems)
**Exact (R²≥0.999):** 2/5 (40%) | **Good (R²≥0.95):** 2/5 (40%) | **Mean R²:** 0.6029
| Problem | Target Formula | R² | MSE | Grade | Discovered Expression | Time |
|---------|---------------|-----|-----|-------|-----------------------|------|
| Korns_Korns1 | `1.57 + 24.3*x3` | 1.000000 | 1.77E-019 | ★★★ Exact | `((0.0646091+x3)*24.3)` | 10.2s |
| Korns_Korns2 | `0.23 + 14.2*(x3+x1)/(3*x4)` | 0.999988 | 5.17E-002 | ★★★ Exact | `((1.03775/(((-13.9243/x1)*x4)/-31.7539))+(((sin((1886.83/...` | 3.3s |
| Korns_Korns3 | `-5.41 + 4.9*(x3-10)*cos(3*x4)/(x3-10)^2` | 0.113025 | 4.91E-001 | ✗ Fail | `(-6.35917+cos((1.23585-sin((0.780466-sin((-1.45663-(x1/x3...` | 2.7s |
| Korns_Korns4 | `-2.3 + 0.13*sin(x2)` | 0.000080 | 8.25E-003 | ✗ Fail | `(cos((0.00971259/x2))*-2.29786)` | 4.9s |
| Korns_Korns5 | `3.0 + 2.13*ln(|x3|)` | 0.901606 | 3.51E-001 | ~ Fair | `(sin((log((0.0596401+(x3/70.2764)))+1.62688))+(sin(log((0...` | 3.2s |

### Nguyen (12 problems)
**Exact (R²≥0.999):** 8/12 (67%) | **Good (R²≥0.95):** 12/12 (100%) | **Mean R²:** 0.9954
| Problem | Target Formula | R² | MSE | Grade | Discovered Expression | Time |
|---------|---------------|-----|-----|-------|-----------------------|------|
| Nguyen_Nguyen1 | `x^3 + x^2 + x` | 1.000000 | 6.54E-008 | ★★★ Exact | `(((-0.314395+cos(((x0-3.41516)/2.72997)))*((expx0.6412)-(...` | 6.4s |
| Nguyen_Nguyen2 | `x^4 + x^3 + x^2 + x` | 1.000000 | 1.64E-032 | ★★★ Exact | `((((((1+x0)*x0)*x0)+x0)*x0)+x0)` | 14.3s |
| Nguyen_Nguyen3 | `x^5 + x^4 + x^3 + x^2 + x` | 0.999804 | 4.68E-004 | ★★★ Exact | `(((-0.575118+cos(((x0-2.62078)/2.74042)))*(exp(sin((8.120...` | 4.2s |
| Nguyen_Nguyen4 | `x^6 + x^5 + x^4 + x^3 + x^2 + x` | 0.999708 | 4.97E-004 | ★★★ Exact | `(((0.697837+cos(((x0-1.72867)/0.739211)))*(exp(sin((0.817...` | 4.5s |
| Nguyen_Nguyen5 | `sin(x^2)*cos(x) - 1` | 0.997668 | 6.39E-005 | ★★ Excellent | `(0.682283/(-0.738669-sin(cos((-1.63133*cosx0)))))` | 5.8s |
| Nguyen_Nguyen6 | `sin(x) + sin(x + x^2)` | 0.999999 | 5.83E-007 | ★★★ Exact | `(((0.240834+cos(((x0-1.13512)/0.625472)))*(exp(sin((x0-5....` | 5.7s |
| Nguyen_Nguyen7 | `ln(x+1) + ln(x^2+1)` | 0.999958 | 2.83E-005 | ★★★ Exact | `((((sin((x0-3.1739))-cossinx0)/-2.19735)*exp(((((0.602569...` | 4.1s |
| Nguyen_Nguyen8 | `sqrt(x)` | 0.999028 | 2.21E-004 | ★★★ Exact | `(((-0.503579+cos(((x0-6.80752)/6.93911)))*(exp(sin((1.865...` | 4.5s |
| Nguyen_Nguyen9 | `sin(x) + sin(y^2)` | 0.987293 | 4.51E-003 | ★ Good | `((((((x2.18372)+-0.0579863)+-0.0443424)-cosx1)+-0.0852739...` | 4.4s |
| Nguyen_Nguyen10 | `2*sin(x)*cos(y)` | 1.000000 | 2.34E-008 | ★★★ Exact | `(((2.28087*cosx1)*cos(cos((-1175.33-(0.66618*cos((-1174.9...` | 6.5s |
| Nguyen_Nguyen11 | `x^y` | 0.992271 | 5.38E-004 | ★★ Excellent | `cos(((1.03441-((1.10902+(((x0.439654)*logx0)*((x0.408133)...` | 3.4s |
| Nguyen_Nguyen12 | `x^4 - x^3 + y^2/2 - y` | 0.968934 | 7.79E-004 | ★ Good | `(((x1/((sinexpxcosx1*(((-0.580825*x1)+(x0-16.9998))-(cosx...` | 3.5s |

### Pagie (1 problems)
**Exact (R²≥0.999):** 0/1 (0%) | **Good (R²≥0.95):** 1/1 (100%) | **Mean R²:** 0.9509
| Problem | Target Formula | R² | MSE | Grade | Discovered Expression | Time |
|---------|---------------|-----|-----|-------|-----------------------|------|
| Pagie_Pagie1 | `1/(1+x^(-4)) + 1/(1+y^(-4))` | 0.950888 | 1.08E-002 | ★ Good | `((exp(sin(sin(sin(cos((0.575449/x1))))))*cos(sin(((sin((-...` | 3.1s |

### Vladislavleva (5 problems)
**Exact (R²≥0.999):** 1/5 (20%) | **Good (R²≥0.95):** 2/5 (40%) | **Mean R²:** 0.8767
| Problem | Target Formula | R² | MSE | Grade | Discovered Expression | Time |
|---------|---------------|-----|-----|-------|-----------------------|------|
| Vladislavleva_Vlad1_Kotanchek | `exp(-(x-1)^2)/(1.2+(y-2.5)^2)` | 0.810313 | 1.15E-002 | ~ Fair | `exp((exp((-0.818542*x0))*((((x0*(x022.733))+(x1-87.96))/e...` | 2.7s |
| Vladislavleva_Vlad4_Ripple | `exp(-(x-1)^2)/(1+(y-1)^2) + exp(-(x-3)^2)/(1+(y-3)^2)` | 0.823933 | 1.00E-002 | ~ Fair | `sin((sin((-1.75149-(cos((-2.47387-(x1.75144)))-3.97154)))...` | 2.7s |
| Vladislavleva_Vlad5_Salutowicz | `exp(-x)*x^3*cos(x)*sin(x)*(cos(x)*sin(x)^2-1)` | 0.955679 | 4.78E-003 | ★ Good | `(sin((sin((sin(((1.96602*x0)+3.24749))/1.25115))/expsinco...` | 3.0s |
| Vladislavleva_Vlad6_Salutowicz2D | `6*sin(x)*cos(y)` | 1.000000 | 9.57E-026 | ★★★ Exact | `cosxsinx0` | 14.0s |
| Vladislavleva_Vlad7_UBall5D | `10/(5 + sum((xi-3)^2, i=1..5))` | 0.793471 | 5.93E-003 | ✗ Fail | `cos((exp((sin(cos(((0.0610004*x4)+x2)))*0.147561))+((((co...` | 3.0s |

---
# Standard Datasets Benchmark Report

**Total Datasets:** 100
**Average Time per Dataset:** 0.66 seconds

## Summary by Difficulty
| Difficulty | Avg R2 | Avg R2 (No-Noise) | Success Rate (R2 > 0.95) |
|------------|--------|-------------------|--------------------------| 
| Simple | 0.9295 | 0.9465 | 60.0% |
| Rational | 0.8623 | 0.9138 | 45.0% |
| Transcendental | 0.8308 | 0.9416 | 44.0% |
| Feynman | 0.9588 | 0.9664 | 65.0% |
| Pathological | 0.4875 | 0.5086 | 33.3% |
