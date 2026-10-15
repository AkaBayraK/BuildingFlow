import Svg, { Path } from 'react-native-svg';
import geometry from '../../assets/brand/mark.json';
type Tone = keyof typeof geometry.colors;
export function BuildingFlowMark({ size = 32, variant = 'primary' }: { size?: number; variant?: 'primary' | 'light' | 'mono' }) {
  const colors = variant === 'light' ? { navy: '#F5FAFF', blue: '#69BFE4', teal: '#8BD0D6' } : variant === 'mono' ? { navy: '#082B49', blue: '#082B49', teal: '#082B49' } : geometry.colors;
  return <Svg width={size} height={size} viewBox={geometry.viewBox} aria-hidden={true} focusable={false}>
    {geometry.paths.map((path, index) => <Path key={index} d={path.d} fill="none" stroke={colors[path.tone as Tone]} strokeWidth={geometry.strokeWidth} strokeLinecap="round" strokeLinejoin="round" />)}
  </Svg>;
}
