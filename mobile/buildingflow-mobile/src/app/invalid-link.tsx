import {Link} from 'expo-router';
import {Text} from 'react-native';
import {Screen,styles} from '../components/Screen';
export default function InvalidLink(){return <Screen title="Bağlantı açılamadı"><Text accessibilityRole="alert" style={styles.text}>Bağlantı geçersiz veya çok uzun. Bildirimlerinizi uygulama içinden açabilirsiniz.</Text><Link accessibilityLabel="Ana sayfaya dön" href="/" style={styles.button}><Text style={styles.buttonText}>Ana sayfaya dön</Text></Link></Screen>;}
