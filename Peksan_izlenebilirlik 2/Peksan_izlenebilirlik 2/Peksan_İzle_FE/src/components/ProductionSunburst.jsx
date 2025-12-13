import { useEffect, useRef, useState } from 'react'
import * as d3 from 'd3'
import dataService from '../services/DataService'

const ProductionSunburst = () => {
  const svgRef = useRef()
  const [sequence, setSequence] = useState([])
  const [percentage, setPercentage] = useState(0)
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [showPopup, setShowPopup] = useState(false)
  const [popupData, setPopupData] = useState(null)
  const [popupPosition, setPopupPosition] = useState({ x: 0, y: 0 })
  const [lastUpdated, setLastUpdated] = useState(new Date())
  const [autoRefresh, setAutoRefresh] = useState(true)

  // DataService kullanarak sunburst verilerini çek
  const fetchSunburstData = async (forceRefresh = false) => {
    try {
      setLoading(true)
      const apiData = await dataService.getSunburstData(forceRefresh)

      setData(apiData)
      setLastUpdated(apiData.lastUpdated)
    } catch (error) {
      console.error('Sunburst veri yükleme hatası:', error)
    } finally {
      setLoading(false)
    }
  }

  // Component mount olduğunda veri çek
  useEffect(() => {
    fetchSunburstData()

    // DataService'den veri güncellemelerini dinle
    const unsubscribe = dataService.subscribe('sunburst', (data) => {
      setData(data)
      setLastUpdated(data.lastUpdated)
    })

    // Cleanup
    return () => {
      unsubscribe()
    }
  }, [])

  // Otomatik yenileme durumu değiştiğinde DataService'i güncelle
  useEffect(() => {
    dataService.setAutoRefresh(autoRefresh)
  }, [autoRefresh])

  // Ay sıralaması için yardımcı fonksiyon
  const getMonthOrder = (monthName) => {
    const months = {
      "Ocak": 1, "Şubat": 2, "Mart": 3, "Nisan": 4, "Mayıs": 5, "Haziran": 6,
      "Temmuz": 7, "Ağustos": 8, "Eylül": 9, "Ekim": 10, "Kasım": 11, "Aralık": 12
    }
    return months[monthName] || 0
  }

  useEffect(() => {
    if (!svgRef.current || !data || loading) return

    // Clear previous chart
    d3.select(svgRef.current).selectAll("*").remove()

    const width = 600
    const height = 600
    const radius = Math.min(width, height) / 2

    // Create SVG
    const svg = d3.select(svgRef.current)
      .attr("width", width)
      .attr("height", height)

    const g = svg.append("g")
      .attr("transform", `translate(${width / 2},${height / 2})`)

    // Create partition layout
    const partition = d3.partition()
      .size([2 * Math.PI, radius])

    // Create hierarchy
    const root = d3.hierarchy(data)
      .sum(d => d.size || 0)
      // Özel sıralama: Önce kategorileri alfabetik, sonra ayları kronolojik sırala
      .sort((a, b) => {
        // Eğer aynı seviyedeyse
        if (a.depth === b.depth) {
          // Eğer ay isimleri ise, ay sırasına göre sırala
          if (a.depth === 2) {
            return getMonthOrder(a.data.name) - getMonthOrder(b.data.name)
          }
          // Diğer durumlarda alfabetik sırala
          return a.data.name.localeCompare(b.data.name)
        }
        // Farklı seviyedeyse değere göre sırala
        return b.value - a.value
      })

    // Apply partition
    partition(root)

    // Color scale
    const color = d3.scaleOrdinal()
      .domain(["Enjeksiyon", "Montaj"])
      .range(["#1f77b4", "#ff7f0e"])

    // Arc generator
    const arc = d3.arc()
      .startAngle(d => d.x0)
      .endAngle(d => d.x1)
      .innerRadius(d => d.y0)
      .outerRadius(d => d.y1)

    // Create arcs
    g.selectAll("path")
      .data(root.descendants().filter(d => d.depth > 0))
      .enter()
      .append("path")
      .attr("d", arc)
      .style("fill", d => {
        if (d.depth === 1) {
          return color(d.data.name)
        } else {
          // For children, use different colors based on future/past
          const parentColor = color(d.parent.data.name)
          if (d.data.details && d.data.details.isFutureMonth) {
            // Gelecek aylar için çok soluk renk
            return d3.color(parentColor).brighter(1.5).toString() + "40" // 40 = opacity
          } else {
            // Geçmiş aylar için normal açık renk
            return d3.color(parentColor).brighter(0.5)
          }
        }
      })
      .style("stroke", "#fff")
      .style("stroke-width", 2)
      .style("cursor", d => {
        // Gelecek aylar için cursor değiştir
        if (d.depth === 2 && d.data.details && d.data.details.isFutureMonth) {
          return "not-allowed"
        }
        return "pointer"
      })
      .style("opacity", d => {
        // Gelecek aylar için opacity düşür
        if (d.depth === 2 && d.data.details && d.data.details.isFutureMonth) {
          return 0.4
        }
        return 1
      })
      .on("mouseover", function(event, d) {
        // Highlight path
        const sequenceArray = d.ancestors().reverse()

        // Gelecek aylar için özel mesaj
        if (d.depth === 2 && d.data.details && d.data.details.isFutureMonth) {
          setSequence([...sequenceArray.map(node => node.data.name), "Üretim Bekleniyor"])
          setPercentage(0)
        } else {
          setSequence(sequenceArray.map(node => node.data.name))
          setPercentage(((d.value / root.value) * 100).toFixed(1))
        }

        // Fade all other arcs
        g.selectAll("path")
          .style("opacity", pathData => {
            // Gelecek aylar için farklı opacity
            if (pathData.depth === 2 && pathData.data.details && pathData.data.details.isFutureMonth) {
              return 0.1
            }
            return 0.3
          })

        // Highlight current path
        sequenceArray.forEach(node => {
          g.selectAll("path")
            .filter(pathData => pathData === node)
            .style("opacity", pathData => {
              // Gelecek aylar için bile highlight edildiğinde biraz daha görünür yap
              if (pathData.depth === 2 && pathData.data.details && pathData.data.details.isFutureMonth) {
                return 0.6
              }
              return 1
            })
        })
      })
      .on("mouseout", function() {
        // Reset all arcs to their original opacity
        g.selectAll("path")
          .style("opacity", d => {
            // Gelecek aylar için orijinal düşük opacity'yi geri getir
            if (d.depth === 2 && d.data.details && d.data.details.isFutureMonth) {
              return 0.4
            }
            return 1
          })
        setSequence([])
        setPercentage(0)
      })
      .on("click", function(event, d) {
        // Sadece geçmiş ay segmentleri için pop-up göster (depth === 2 ve gelecek ay değil)
        if (d.depth === 2 && d.data.details && !d.data.details.isFutureMonth) {
          // Segment'in merkez koordinatını hesapla
          const angle = (d.x0 + d.x1) / 2
          const radius = (d.y0 + d.y1) / 2
          const x = Math.sin(angle) * radius
          const y = -Math.cos(angle) * radius

          // SVG'nin container içindeki konumunu al
          const svgRect = svgRef.current.getBoundingClientRect()
          const containerRect = svgRef.current.closest('.d3-sunburst-container').getBoundingClientRect()

          // SVG merkezi + segment pozisyonu
          const centerX = svgRect.width / 2
          const centerY = svgRect.height / 2

          setPopupPosition({
            x: (svgRect.left - containerRect.left) + centerX + x,
            y: (svgRect.top - containerRect.top) + centerY + y
          })

          setPopupData({
            monthName: d.data.details.monthName,
            categoryName: d.data.details.categoryName,
            productionCount: d.data.details.productionCount,
            percentage: ((d.value / root.value) * 100).toFixed(1),
            isFutureMonth: d.data.details.isFutureMonth,
            statusMessage: d.data.details.statusMessage
          })

          setShowPopup(true)
        }
      })

    // Add center text
    const centerText = g.append("text")
      .attr("text-anchor", "middle")
      .attr("dy", "0.35em")
      .style("font-size", "14px")
      .style("font-weight", "bold")
      .style("fill", "#333")
      .text("Peksan Üretim")

    // Ana kategori isimlerini ekle (Enjeksiyon, Montaj)
    g.selectAll(".category-label")
      .data(root.descendants().filter(d => d.depth === 1)) // Ana kategoriler
      .enter()
      .append("text")
      .attr("class", "category-label")
      .attr("transform", d => {
        // Segmentin ortasında konumlandır
        const angle = (d.x0 + d.x1) / 2
        const radius = (d.y0 + d.y1) / 2
        const x = Math.sin(angle) * radius
        const y = -Math.cos(angle) * radius
        // Metni döndür
        const rotate = (angle * 180 / Math.PI) - 90
        return `translate(${x},${y}) rotate(${rotate})`
      })
      .attr("text-anchor", "middle")
      .attr("dy", "0.35em")
      .style("font-size", "12px")
      .style("font-weight", "bold")
      .style("fill", "#fff")
      .style("pointer-events", "none") // Tıklamayı engelle
      .text(d => d.data.name)

    // Ay isimlerini ekle (sadece ay segmentleri için)
    g.selectAll(".month-label")
      .data(root.descendants().filter(d => d.depth === 2)) // Sadece aylar
      .enter()
      .append("text")
      .attr("class", "month-label")
      .attr("transform", d => {
        // Segmentin ortasında konumlandır
        const angle = (d.x0 + d.x1) / 2
        const radius = (d.y0 + d.y1) / 2
        const x = Math.sin(angle) * radius
        const y = -Math.cos(angle) * radius
        // Metni döndür
        const rotate = (angle * 180 / Math.PI) - 90
        return `translate(${x},${y}) rotate(${rotate})`
      })
      .attr("text-anchor", "middle")
      .attr("dy", "0.35em")
      .style("font-size", "10px")
      .style("font-weight", "bold")
      .style("fill", "#fff")
      .style("pointer-events", "none") // Tıklamayı engelle
      .text(d => d.data.name)

  }, [data, loading])

  return (
    <div className="d3-sunburst-container">
      <div className="sunburst-header">
        <h3 className="sunburst-title">Üretim Dağılımı - İnteraktif Sunburst</h3>

        <div className="refresh-controls">
          <div className="auto-refresh-toggle">
            <label>
              <input
                type="checkbox"
                checked={autoRefresh}
                onChange={(e) => setAutoRefresh(e.target.checked)}
              />
              Otomatik Yenileme (10dk)
            </label>
          </div>

          <button
            className="manual-refresh-btn"
            onClick={() => fetchSunburstData(true)}
            disabled={loading}
          >
            {loading ? 'Yenileniyor...' : 'Manuel Yenile'}
          </button>

          <div className="last-updated">
            Son Güncelleme: {lastUpdated.toLocaleTimeString()}
          </div>
        </div>
      </div>

      {loading && (
        <div className="loading-message">
          <p>Veriler yükleniyor...</p>
        </div>
      )}

      {/* Sequence display */}
      <div className="sequence-container">
        {sequence.length > 0 && (
          <div className="sequence">
            <span className="sequence-text">
              {sequence.join(" → ")}
              {percentage > 0 && <span className="percentage"> ({percentage}%)</span>}
            </span>
          </div>
        )}
      </div>

      {/* SVG Chart */}
      <div className="chart-wrapper">
        <svg ref={svgRef}></svg>
      </div>

      {/* Pop-up */}
      {showPopup && popupData && (
        <div
          className="production-popup"
          style={{
            position: 'absolute',
            left: Math.min(popupPosition.x + 10, window.innerWidth - 320), // Ekran dışına çıkmasını engelle
            top: Math.max(popupPosition.y - 10, 10), // Üst sınırı kontrol et
            zIndex: 1000
          }}
        >
          <div className="popup-content">
            <button
              className="popup-close"
              onClick={() => setShowPopup(false)}
            >
              ×
            </button>
            <h4>{popupData.monthName} {popupData.categoryName}</h4>
            <div className="popup-details">
              {popupData.isFutureMonth ? (
                <>
                  <p><strong>Durum:</strong> <span className="future-status">{popupData.statusMessage}</span></p>
                  <p><strong>Üretim Adedi:</strong> 0 koli</p>
                  <p><strong>Toplam İçindeki Payı:</strong> 0%</p>
                </>
              ) : (
                <>
                  <p><strong>Üretim Adedi:</strong> {popupData.productionCount.toLocaleString()} koli</p>
                  <p><strong>Toplam İçindeki Payı:</strong> {popupData.percentage}%</p>
                </>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Legend */}
      <div className="legend">
        <div className="legend-item">
          <div className="legend-color" style={{backgroundColor: "#1f77b4"}}></div>
          <span>Enjeksiyon</span>
        </div>
        <div className="legend-item">
          <div className="legend-color" style={{backgroundColor: "#ff7f0e"}}></div>
          <span>Montaj</span>
        </div>
      </div>
    </div>
  )
}

export default ProductionSunburst
