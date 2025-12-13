// Global veri yönetim servisi
class DataService {
  constructor() {
    this.dashboardData = null
    this.sunburstData = null
    this.lastDashboardUpdate = null
    this.lastSunburstUpdate = null
    this.refreshInterval = 10 * 60 * 1000 // 10 dakika
    this.intervalId = null
    this.isAutoRefreshEnabled = true
    this.subscribers = {
      dashboard: [],
      sunburst: []
    }
    
    // Otomatik yenilemeyi başlat
    this.startAutoRefresh()
  }

  // Subscriber pattern - bileşenler veri değişikliklerini dinleyebilir
  subscribe(dataType, callback) {
    if (this.subscribers[dataType]) {
      this.subscribers[dataType].push(callback)
    }
    
    // Cleanup fonksiyonu döndür
    return () => {
      if (this.subscribers[dataType]) {
        this.subscribers[dataType] = this.subscribers[dataType].filter(cb => cb !== callback)
      }
    }
  }

  // Subscribers'ları bilgilendir
  notifySubscribers(dataType, data) {
    if (this.subscribers[dataType]) {
      this.subscribers[dataType].forEach(callback => callback(data))
    }
  }

  // Dashboard verilerini getir
  async getDashboardData(forceRefresh = false) {
    const now = new Date()
    
    // Cache kontrolü - 10 dakika geçmemişse cache'den döndür
    if (!forceRefresh && this.dashboardData && this.lastDashboardUpdate) {
      const timeDiff = now - this.lastDashboardUpdate
      if (timeDiff < this.refreshInterval) {
        console.log('Dashboard verileri cache\'den döndürülüyor')
        return this.dashboardData
      }
    }

    try {
      console.log('Dashboard verileri API\'den çekiliyor...', new Date().toLocaleTimeString())

      // Timeout ile fetch - 10 saniye timeout
      const controller = new AbortController()
      const timeoutId = setTimeout(() => controller.abort(), 10000)

      const response = await fetch('http://localhost:5022/api/Dashboard', {
        signal: controller.signal,
        headers: {
          'Content-Type': 'application/json',
        }
      })

      clearTimeout(timeoutId)
      console.log('Dashboard API yanıtı alındı:', new Date().toLocaleTimeString())

      if (response.ok) {
        const data = await response.json()
        
        // Makine tipine göre günlük üretim verilerini ayır
        let enjeksiyonKoli = 0
        let montajKoli = 0
        let serigrafKoli = 0
        
        if (data.dailyProductionDetails) {
          data.dailyProductionDetails.forEach(item => {
            if (item.makineType === 'Enjeksiyon') {
              enjeksiyonKoli += item.uretilenKoli
            } else if (item.makineType === 'Montaj') {
              montajKoli += item.uretilenKoli
            } else if (item.makineType === 'Serigrafi') {
              serigrafKoli += item.uretilenKoli
            }
          })
        }
        
        this.dashboardData = {
          totalProduction: data.totalProduction,
          rawMaterialStock: data.rawMaterialStock,
          dailyProduction: data.dailyProduction,
          dailyEnjeksiyon: enjeksiyonKoli,
          dailyMontaj: montajKoli,
          dailySerigrafi: serigrafKoli,
          lastUpdated: now
        }
        
        this.lastDashboardUpdate = now
        
        // Subscribers'ları bilgilendir
        this.notifySubscribers('dashboard', this.dashboardData)
        
        console.log('Dashboard verileri güncellendi:', now.toLocaleTimeString())
        return this.dashboardData
      } else {
        throw new Error('Dashboard API yanıtı başarısız')
      }
    } catch (error) {
      console.error('Dashboard API çağrısı hatası:', error)

      // Timeout hatası kontrolü
      if (error.name === 'AbortError') {
        console.error('Dashboard API timeout - 10 saniye aşıldı')
      }

      // Hata durumunda mevcut cache'i döndür
      if (this.dashboardData) {
        console.log('Cache\'den dashboard verisi döndürülüyor')
        return this.dashboardData
      }

      // Fallback data - backend çalışmıyorsa
      const fallbackData = {
        totalProduction: 0,
        rawMaterialStock: 0,
        dailyProduction: 0,
        dailyEnjeksiyon: 0,
        dailyMontaj: 0,
        dailySerigrafi: 0,
        lastUpdated: now
      }

      console.log('Fallback dashboard verisi döndürülüyor')
      this.dashboardData = fallbackData
      this.notifySubscribers('dashboard', fallbackData)
      return fallbackData
    }
  }

  // Sunburst verilerini getir
  async getSunburstData(forceRefresh = false) {
    const now = new Date()
    
    // Cache kontrolü - 10 dakika geçmemişse cache'den döndür
    if (!forceRefresh && this.sunburstData && this.lastSunburstUpdate) {
      const timeDiff = now - this.lastSunburstUpdate
      if (timeDiff < this.refreshInterval) {
        console.log('Sunburst verileri cache\'den döndürülüyor')
        return this.sunburstData
      }
    }

    try {
      console.log('Sunburst verileri API\'den çekiliyor...')
      const response = await fetch('http://localhost:5022/api/Dashboard/sunburst-data')
      
      if (response.ok) {
        const data = await response.json()
        
        this.sunburstData = {
          ...data,
          lastUpdated: now
        }
        
        this.lastSunburstUpdate = now
        
        // Subscribers'ları bilgilendir
        this.notifySubscribers('sunburst', this.sunburstData)
        
        console.log('Sunburst verileri güncellendi:', now.toLocaleTimeString())
        return this.sunburstData
      } else {
        throw new Error('Sunburst API yanıtı başarısız')
      }
    } catch (error) {
      console.error('Sunburst API çağrısı hatası:', error)
      
      // Hata durumunda mevcut cache'i döndür
      if (this.sunburstData) {
        return this.sunburstData
      }
      
      // Fallback data
      const fallbackData = {
        name: "Peksan Üretim",
        children: [
          {
            name: "Enjeksiyon",
            children: [
              { name: "Ocak", size: 0 },
              { name: "Şubat", size: 0 }
            ]
          },
          {
            name: "Montaj",
            children: [
              { name: "Ocak", size: 0 },
              { name: "Şubat", size: 0 }
            ]
          }
        ],
        lastUpdated: now
      }
      
      this.sunburstData = fallbackData
      this.notifySubscribers('sunburst', fallbackData)
      return fallbackData
    }
  }

  // Tüm verileri yenile
  async refreshAllData() {
    console.log('Tüm veriler yenileniyor...')
    
    try {
      await Promise.all([
        this.getDashboardData(true),
        this.getSunburstData(true)
      ])
      
      console.log('Tüm veriler başarıyla yenilendi')
    } catch (error) {
      console.error('Veri yenileme hatası:', error)
    }
  }

  // Otomatik yenilemeyi başlat
  startAutoRefresh() {
    if (this.intervalId) {
      clearInterval(this.intervalId)
    }
    
    if (this.isAutoRefreshEnabled) {
      this.intervalId = setInterval(() => {
        this.refreshAllData()
      }, this.refreshInterval)
      
      console.log('Otomatik yenileme başlatıldı (10 dakika aralıklarla)')
    }
  }

  // Otomatik yenilemeyi durdur
  stopAutoRefresh() {
    if (this.intervalId) {
      clearInterval(this.intervalId)
      this.intervalId = null
      console.log('Otomatik yenileme durduruldu')
    }
  }

  // Otomatik yenileme durumunu değiştir
  setAutoRefresh(enabled) {
    this.isAutoRefreshEnabled = enabled
    
    if (enabled) {
      this.startAutoRefresh()
    } else {
      this.stopAutoRefresh()
    }
  }

  // Son güncelleme zamanlarını getir
  getLastUpdateTimes() {
    return {
      dashboard: this.lastDashboardUpdate,
      sunburst: this.lastSunburstUpdate
    }
  }

  // Cache'i temizle
  clearCache() {
    this.dashboardData = null
    this.sunburstData = null
    this.lastDashboardUpdate = null
    this.lastSunburstUpdate = null
    console.log('Cache temizlendi')
  }

  // Service'i temizle
  destroy() {
    this.stopAutoRefresh()
    this.clearCache()
    this.subscribers = { dashboard: [], sunburst: [] }
    console.log('DataService temizlendi')
  }
}

// Singleton instance
const dataService = new DataService()

export default dataService
