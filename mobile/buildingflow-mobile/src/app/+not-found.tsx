import { Link } from 'expo-router';
import { Text } from 'react-native';
import { Screen, styles } from '../components/Screen';
export default function NotFound() { return <Screen title="Ekran bulunamadı"><Text style={styles.text}>Bu bağlantı geçerli bir ekrana gitmiyor.</Text><Link href="/" style={styles.text}>Ana sayfaya dön</Link></Screen>; }
